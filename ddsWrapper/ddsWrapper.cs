#define newCalc

using Bridge;
using System.Numerics;
using DDS.Interop;

namespace DDS
{
    public static unsafe partial class ddsWrapper
    {
        // atomic bitmask: bit==1 means occupied
        private static int threadMask = 0;
        private static readonly int maxThreads;
        private static readonly Lock maskLock = new();

        // per-managed-thread cached index; -1 means none claimed yet
        private static readonly ThreadLocal<int> threadLocalIndex = new(() => -1);

        // Thread-local pooled List to avoid repeated small allocations and internal array growth.
        // Each managed thread gets its own buffer; we Clear() and reuse it.  We return a fresh List copy to callers.
        private static readonly ThreadLocal<List<CardPotential>> listPool = new(() => new List<CardPotential>(16));
        private static readonly ThreadLocal<List<TableResults>> tableResultsPool = new(() => new List<TableResults>(16));

        static ddsWrapper()
        {
            maxThreads = ddsImports.MaxThreads;
            if (maxThreads <= 0 || maxThreads > 32)
            {
                throw new InvalidOperationException($"ddsImports.MaxThreads must be between 1 and 32 (was {maxThreads})");
            }
        }

        private static unsafe List<CardPotential> SolveBoard(in GameState state, int target, int solutions, int mode)
        {
            // Parameter ”target” is the number of tricks to be won by the side to play, 
            // -1 means that the program shall find the maximum number.
            // For equivalent  cards only the highest is returned.
            // target=1-13, solutions=1:  Returns only one of the cards. 
            // Its returned score is the same as target when target or higher tricks can be won. 
            // Otherwise, score –1 is returned if target cannot be reached, or score 0 if no tricks can be won. 
            // target=-1, solutions=1:  Returns only one of the optimum cards and its score.

            var result = listPool.Value!;
            result.Clear();

            var playedCards = DdsEnum.Convert(state.PlayedByMan1, state.PlayedByMan2, state.PlayedByMan3);
            var deal = DdsInteropConverters.ToInteropDeal(DdsEnum.Convert(state.Trump), DdsEnum.Convert(state.TrickLeader), 
                                playedCards,
                                state.RemainingCards);
            FutureTricks futureTricks = default;
            var threadIndex = GetThreadIndex(); // fast (cached) on repeated calls

            int hresult;
            try
            {
                hresult = ddsImports.SolveBoard(deal, target, solutions, mode, ref futureTricks, threadIndex);
            }
            finally
            {
                // NOTE: we intentionally do NOT release the index here.
                // The index is cached per managed thread to avoid repeated allocation overhead.
                // This is safe only when the number of concurrent managed threads using the library
                // never exceeds ddsImports.MaxThreads.
            }

            ddsImports.ThrowIfError(hresult, nameof(ddsImports.SolveBoard));

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

            // Returns cached per-thread index or claims a new one with CAS on threadMask
            static int GetThreadIndex()
            {
                var cached = threadLocalIndex.Value;
                if (cached != -1) return cached;

                // Try to claim a free bit
                lock (maskLock)
                {
                    var maskAll = (maxThreads == 32) ? unchecked((int)0xFFFFFFFF) : ((1 << maxThreads) - 1);
                    while (true)
                    {
                        int mask = threadMask;
                        int avail = (~mask) & maskAll;
                        if (avail == 0)
                        {
                            throw new InvalidOperationException($"all threads are in use");
                        }

                        int bit = BitOperations.TrailingZeroCount(avail);
                        int bitMask = 1 << bit;
                        int newMask = mask | bitMask;

                        // try to set the bit
                        var old = Interlocked.CompareExchange(ref threadMask, newMask, mask);
                        if (old == mask)
                        {
                            threadLocalIndex.Value = bit;
                            return bit;
                        }
                    }
                }
            }

            //// Optionally free a specific index (not used on the hot path)
            //static void ReleaseIndex(int idx)
            //{
            //    if (idx < 0 || idx >= maxThreads) return;

            //    while (true)
            //    {
            //        int mask = threadMask;
            //        int newMask = mask & ~(1 << idx);
            //        var old = Interlocked.CompareExchange(ref threadMask, newMask, mask);
            //        if (old == mask) return;
            //    }
            //}
        }

        // Use carefully: should only be called when there are no active SolveBoard calls
        public static void ForgetPreviousBoard()
        {
            _ = ddsImports.FreeMemory();
            var max = ddsImports.MaxThreads;
            ddsImports.SetResources(1000, max);

            // reset the global mask; caller must ensure no concurrent SolveBoard calls
            Interlocked.Exchange(ref threadMask, 0);

            // reset per-thread cache for current thread only (other threads will still have their caches;
            // it's caller responsibility to ensure no active threads exist when calling this).
            threadLocalIndex.Value = -1;
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
            for (Hand hand = Hand.North; hand <= Hand.West; hand++)
            {
                var seat = DdsEnum.Convert(hand);
                for (Suit suit = Suit.Spades; suit <= Suit.NT; suit++)
                {
                    result[seat, DdsEnum.Convert(suit)] = results[(int)hand, (int)suit];
                }
            }
            return result;
        }

        public static TableResults PossibleTricks(in Deal _deal)
        {
            var deal = DdsInteropConverters.ToInteropTableDeal(_deal);
            var results = new ddTableResults();
            var hresult = ddsImports.CalcDDtable(deal, ref results);
            ddsImports.ThrowIfError(hresult, nameof(ddsImports.CalcDDtable));

            TableResults result;
            for (Hand hand = Hand.North; hand <= Hand.West; hand++)
            {
                var seat = DdsEnum.Convert(hand);
                for (Suit suit = Suit.Spades; suit <= Suit.NT; suit++)
                {
                    result[seat, DdsEnum.Convert(suit)] = results[(int)hand, (int)suit];
                }
            }
            return result;
        }

        public static List<TableResults> PossibleTricks(in List<Deal> deals, in List<Suits> trumps)
        {
            if (trumps == null || trumps.Count == 0)
            {
                throw new ArgumentException("trumps must contain at least one suit");
            }

            var result = tableResultsPool.Value!;
            result.Clear();

            var tableDeals = DdsInteropConverters.ToInteropTableDeals(in deals);
            var results = new ddTablesResult(deals.Count, trumps.Count);
            var parResults = new allParResults();

            var hresult = ddsImports.CalcAllTables(tableDeals, -1, Convert(in trumps!), ref results, ref parResults);
            ddsImports.ThrowIfError(hresult, nameof(ddsImports.CalcAllTables));

            for (int deal = 0; deal < deals.Count; deal++)
            {
                TableResults tableResult;
                for (Hand hand = Hand.North; hand <= Hand.West; hand++)
                {
                    for (Suit suit = Suit.Spades; suit <= Suit.NT; suit++)
                    {
                        tableResult[DdsEnum.Convert(hand), DdsEnum.Convert(suit)] = results[deal, (int)hand, (int)suit];
                    }
                }
                result.Add(tableResult);
            }

            return result;
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
