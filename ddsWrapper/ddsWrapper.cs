using Bridge;
using DDS.Interop;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;

namespace DDS
{
    public static unsafe partial class ddsWrapper
    {
        // --- low-contention thread-index pool (replaces lock + ManualResetEvent pattern) ---
        private static readonly SemaphoreSlim threadSemaphore;
        private static readonly ConcurrentStack<int> threadStack;

        // Thread-local flag: true when this managed thread acquired an index via semaphore.Wait()
        private static readonly ThreadLocal<bool> semaphoreAcquired = new(() => false);
        private static readonly int maxThreads;
        private static readonly bool[] threadOccupied = new bool[16];

        // Thread-local pooled List to avoid repeated small allocations and internal array growth.
        // Each managed thread gets its own buffer; we Clear() and reuse it.  We return a fresh List copy to callers.
        private static readonly ThreadLocal<List<CardPotential>> listPool = new(() => new List<CardPotential>(16));
        private static readonly ThreadLocal<List<TableResults>> tableResultsPool = new(() => new List<TableResults>(16));

        // Put these in DdsInteropConverters or a nearby static helper class
        private static readonly int[] SuitMap;

        static ddsWrapper()
        {
            maxThreads = ddsImports.MaxThreads;
            if (maxThreads <= 0 || maxThreads > 32)
            {
                throw new InvalidOperationException($"ddsImports.MaxThreads must be between 1 and 32 (was {maxThreads})");
            }

            // initialize low-contention pool
            threadSemaphore = new SemaphoreSlim(maxThreads, maxThreads);
            threadStack = new ConcurrentStack<int>();
            for (int i = 0; i < maxThreads; i++) threadStack.Push(i);

            // Suits mapping initialization (unchanged)
            // Suits: Spades..NT -> DdsEnum.Convert
            // Note: your Suit enum ordering may differ; adjust start/length accordingly.
            SuitMap = new int[Enum.GetValues(typeof(Suit)).Length];
            for (int s = 0; s < SuitMap.Length; s++)
                SuitMap[s] = (int)DdsEnum.Convert((Suit)s);
        }

        private static unsafe List<CardPotential> SolveBoard(in GameState state, int target, int solutions, int mode)
        {
            var result = listPool.Value!;
            result.Clear();

            var playedCards = DdsEnum.Convert(state.PlayedByMan1, state.PlayedByMan2, state.PlayedByMan3);
            var deal = DdsInteropConverters.ToInteropDeal(DdsEnum.Convert(state.Trump), DdsEnum.Convert(state.TrickLeader),
                            playedCards,
                            state.RemainingCards);
            FutureTricks futureTricks = default;

            var threadIndex = GetThreadIndex();
            int hresult;
            try
            {
                hresult = ddsImports.SolveBoard(deal, target, solutions, mode, ref futureTricks, threadIndex);
            }
            finally
            {
                ReleaseThreadIndex(threadIndex);
            }

            if (hresult < 0)
            {
                var error = ddsImports.GetErrorMessage(hresult);
                throw new ExternalException($"{nameof(ddsImports.SolveBoard)} failed with code {hresult}: {error}. {state.RemainingCards.ToPBN()} {state.PlayedByMan1} {state.PlayedByMan2} {state.PlayedByMan3}", hresult);
            }

            // Remove the fixed statement for already fixed pointers (FutureTricks fields are already pointers)
            int* suitPtr = futureTricks.suit;
            int* rankPtr = futureTricks.rank;
            int* equalsPtr = futureTricks.equals;
            int* scorePtr = futureTricks.score;
            {
                for (int i = 0; i < futureTricks.cards; i++)
                {
                    var ddsSuit = (Suit)suitPtr[i];
                    var suit = DdsEnum.Convert(ddsSuit);
                    var rank = (Rank)rankPtr[i];
                    var score = scorePtr[i];
                    var eqMask = (uint)equalsPtr[i];

                    // highest card (primary)
                    result.Add(new CardPotential(Bridge.Card.Get(suit, DdsEnum.Convert(rank)), score, eqMask == 0));

                    // iterate equivalent lower ranks using bit scanning
                    if (eqMask != 0)
                    {
                        bool firstEqual = true;
                        // eqMask uses bit positions for ranks; adjust mapping if needed.
                        while (eqMask != 0)
                        {
                            int bit = BitOperations.TrailingZeroCount(eqMask);
                            eqMask &= eqMask - 1;
                            var eqRank = (Rank)(bit + 2 - (int)Rank.Two);
                            result.Add(new CardPotential(Bridge.Card.Get(suit, DdsEnum.Convert(eqRank)), score, firstEqual));
                            firstEqual = false;
                        }
                    }
                }
            }

            return result;

            // low-contention GetThreadIndex / ReleaseThreadIndex local functions
            int GetThreadIndex()
            {
                // Fast-path: try to get an index without blocking
                if (threadStack.TryPop(out var idx))
                {
                    // did not consume a semaphore permit
                    semaphoreAcquired.Value = false;
                    return idx;
                }

                // Slow-path: wait for a permit, then pop an index
                threadSemaphore.Wait();

                // mark that this managed thread acquired a permit
                semaphoreAcquired.Value = true;

                // There is guaranteed to be an index available, but TryPop may still fail transiently under races.
                // Spin briefly until successful; this loop will be very short.
                while (!threadStack.TryPop(out idx))
                {
                    Thread.SpinWait(1);
                }
                return idx;
            }

            void ReleaseThreadIndex(int threadIndex)
            {
                // push back
                threadStack.Push(threadIndex);

                // only release the semaphore if this managed thread previously waited for a permit
                if (semaphoreAcquired.Value)
                {
                    semaphoreAcquired.Value = false;
                    threadSemaphore.Release();
                }
            }
        }

        // Use carefully: should only be called when there are no active SolveBoard calls
        public static void ForgetPreviousBoard()
        {
            _ = ddsImports.FreeMemory();
            var max = ddsImports.MaxThreads;
            ddsImports.SetResources(1000, max);

            // Reset the semaphore to 'max' permits and repopulate the stack.
            // This is only safe if there are no concurrent SolveBoard calls.
            while (threadSemaphore.CurrentCount < max) threadSemaphore.Release();

            // empty the stack then push 0..max-1
            while (threadStack.TryPop(out _)) { }
            for (int i = 0; i < max; i++) threadStack.Push(i);
        }

        public static List<CardPotential> BestCards(in GameState state)
        {
            return SolveBoard(in state, -1, 2, 1);
        }

        public static CardPotential BestCard(in GameState state)
        {
            return SolveBoard(in state, -1, 1, 1)[0];
        }

        public static List<CardPotential> AllCards(in GameState state)
        {
            return SolveBoard(in state, 0, 3, 1);
        }

        public static TableResults PossibleTricks(in string pbn)
        {
            var deal = DdsInteropConverters.ToInteropTableDealPbn(pbn);
            var results = new ddTableResults();
            var hresult = ddsImports.CalcDDtablePBN(deal, ref results);
            ddsImports.ThrowIfError(hresult, nameof(ddsImports.CalcDDtablePBN));

            TableResults result;
            CopySingleTableResults(in results, ref result);
            return result;
        }

        public static TableResults PossibleTricks(in Deal _deal)
        {
            var deal = DdsInteropConverters.ToInteropTableDeal(_deal);
            var results = new ddTableResults();
            var hresult = ddsImports.CalcDDtable(deal, ref results);
            ddsImports.ThrowIfError(hresult, nameof(ddsImports.CalcDDtable));

            TableResults result;
            CopySingleTableResults(in results, ref result);
            return result;
        }

        // Copy single ddTableResults -> TableResults
        private static void CopySingleTableResults(in ddTableResults src, ref TableResults dst)
        {
            // assume TableResults is indexable by [seat, suit]
            // use int loops and cached maps to avoid repeated conversions
            for (int hand = 0; hand < 4; hand++)
            {
                for (int suit = 0; suit <= 4; suit++)
                {
                    int suitIndex = SuitMap[suit];
                    dst[hand, suitIndex] = src[hand, suit];
                }
            }
        }

        public static List<TableResults> PossibleTricks(in List<Deal> deals, in List<Suits> trumps)
        {
            if (trumps == null || trumps.Count == 0)
                throw new ArgumentException("trumps must contain at least one suit");

            var result = tableResultsPool.Value!;
            result.Clear();

            var parResults = new allParResults();

            int maxDealsPerCall = Math.Max(1, 200 / trumps.Count);
            int total = deals.Count;

            // Process in index-based chunks to avoid Chunk allocations
            for (int offset = 0; offset < total; offset += maxDealsPerCall)
            {
                int len = Math.Min(maxDealsPerCall, total - offset);

                // Use an IReadOnlyList<Deal> slice if you have one; otherwise pass the original list and an offset/length overload.
                // Here we assume ToInteropTableDeals accepts IReadOnlyList<Deal> and an optional (offset,len) overload.
                var tableDeals = DdsInteropConverters.ToInteropTableDeals(deals, offset, len);
                var results = new ddTablesResult(len, trumps.Count);

                var hresult = ddsImports.CalcAllTables(tableDeals, -1, Convert(in trumps!), ref results, ref parResults);
                ddsImports.ThrowIfError(hresult, nameof(ddsImports.CalcAllTables));

                for (int i = 0; i < len; i++)
                {
                    TableResults tableResult = default;
                    CopyTablesResults(in results, i, ref tableResult);
                    result.Add(tableResult);
                }
            }

            return result;
        }

        // Copy ddTablesResult (many deals x many trumps) -> TableResults list
        private static void CopyTablesResults(in ddTablesResult src, int dealIndex, ref TableResults dst)
        {
            for (int hand = 0; hand < 4; hand++)
            {
                for (int suit = 0; suit <= 4; suit++)
                {
                    int suitIndex = SuitMap[suit];
                    dst[hand, suitIndex] = src[dealIndex, hand, suit];
                }
            }
        }

        private static TrumpFilter5 Convert(in List<Suits> trumps)
        {
            TrumpFilter5 result;
            result.values[0] = 1;
            result.values[1] = 1;
            result.values[2] = 1;
            result.values[3] = 1;
            result.values[4] = 1;
            if (trumps == null || trumps.Count == 0)
            {
                result.values[0] = 0;
                result.values[1] = 0;
                result.values[2] = 0;
                result.values[3] = 0;
                result.values[4] = 0;
            }
            else
            {
                foreach (Suits suit in trumps)
                {
                    result.values[(int)DdsEnum.Convert(suit)] = 0;
                }
            }
            return result;
        }
    }
}
