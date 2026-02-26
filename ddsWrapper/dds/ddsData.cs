
// ddsRichTypes.cs
// High-level managed types. Nice domain model, strings, arrays.
// Mapped into blittable interop structs.

using Bridge;
using DDS.Interop;

namespace DDS
{
    internal enum Suit { Spades = 0, Hearts = 1, Diamonds = 2, Clubs = 3, NT = 4 }
    internal enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }
    internal enum Hand { North = 0, East = 1, South = 2, West = 3 }

    internal readonly struct Card(Suit s, Rank r)
    {
        public Suit Suit { get; } = s; public Rank Rank { get; } = r;
    }

    // Example of your played-cards structure
    internal readonly struct PlayedCards(Suit s1, Rank r1, Suit s2, Rank r2, Suit s3, Rank r3)
    {
        public Suit S1 { get; } = s1;
        public Suit S2 { get; } = s2;
        public Suit S3 { get; } = s3;
        public Rank R1 { get; } = r1;
        public Rank R2 { get; } = r2;
        public Rank R3 { get; } = r3;
    }

    // ------------------------
    // Conversion helpers
    // ------------------------

    internal static unsafe class DdsInteropConverters
    {
        // Precompute maps once
        private static readonly int[] SeatMap;
        private static readonly int[] SuitMap;
        private static readonly int[] RankMap;      // maps Ranks.Two..Ace -> 0..12 (or DdsEnum.Convert result)
        private static readonly uint[] RankMask;    // mask for each rankIndex

        static DdsInteropConverters()
        {
            SeatMap = new int[4];
            foreach (Seats seat in SeatsExtensions.SeatsAscending)
                SeatMap[(int)seat] = (int)DdsEnum.Convert(seat);

            SuitMap = new int[4];
            foreach (Suits suit in SuitHelper.StandardSuitsAscending)
                SuitMap[(int)suit] = (int)DdsEnum.Convert(suit);

            // Ranks: Two..Ace
            int rankCount = 13;
            RankMap = new int[rankCount];
            RankMask = new uint[rankCount];
            foreach (Ranks r in RankHelper.RanksAscending)
            {
                int rankIndex = (int)DdsEnum.Convert(r);
                // Map conv to a zero-based index for mask shifting.
                // If DdsEnum.Convert returns 2..14 (matching Ranks), use conv - 2.
                // Adjust here if DdsEnum.Convert uses a different mapping.
                RankMap[(int)r] = rankIndex;
                RankMask[rankIndex - 2] = 1u << rankIndex;
            }
        }

        // Public array overload delegates to the shared implementation
        public static unsafe ddTableDeals ToInteropTableDeals(in Deal[] deals)
            => ToInteropTableDeals((IReadOnlyList<Deal>)deals);

        // Public List overload delegates to the shared implementation
        public static unsafe ddTableDeals ToInteropTableDeals(in List<Deal> deals)
            => ToInteropTableDeals((IReadOnlyList<Deal>)deals);

        // Core implementation that works for arrays, lists, or any IReadOnlyList<Deal>
        private static unsafe ddTableDeals ToInteropTableDeals(IReadOnlyList<Deal> deals)
        {
            int count = deals.Count;
#if DEBUG
            if (count > ddsImports.ddsMaxNumberOfBoards)
                throw new ArgumentOutOfRangeException(nameof(deals),
                    $"Cannot exceed {ddsImports.ddsMaxNumberOfBoards} deals.");
#endif
            ddTableDeals tableDeals = default;
            tableDeals.noOfTables = count;

            // For each deal, fill the 16 uints (4 hands * 4 suits)
            for (int dealIndex = 0; dealIndex < count; dealIndex++)
            {
                Span<uint> dealSpan = tableDeals[dealIndex];
                FillDealSpan(deals[dealIndex], dealSpan);
            }

            return tableDeals;
        }

        public static unsafe ddTableDeals ToInteropTableDeals(in List<Deal> deals, int offset, int len)
        {
            if (deals is null)
                throw new ArgumentNullException(nameof(deals));
#if DEBUG
            if (offset < 0 || len < 0 || offset + len > deals.Count)
                throw new ArgumentOutOfRangeException(nameof(offset), "Invalid offset/len for deals list.");

            if (len > ddsImports.ddsMaxNumberOfBoards)
                throw new ArgumentOutOfRangeException(nameof(len),
                    $"Cannot exceed {ddsImports.ddsMaxNumberOfBoards} deals.");
#endif
            ddTableDeals tableDeals = default;
            tableDeals.noOfTables = len;

            // Fill each slot in the ddTableDeals directly from the list slice
            for (int i = 0; i < len; i++)
            {
                // Get the span for this deal (16 uints)
                Span<uint> dealSpan = tableDeals[i];

                // Convert the deal at (offset + i) into the span.
                // Reuses the centralized per-deal conversion logic.
                FillDealSpan(deals[offset + i], dealSpan);
            }

            return tableDeals;
        }

        // Helper: convert a single Deal into the 16 uints in dealSpan
        private static void FillDealSpan(Deal deal, Span<uint> dealSpan)
        {
            // Get a ref to the first element to allow Unsafe.Add writes if desired.
            // For clarity we use indexed writes; JIT will optimize bounds checks away in many cases.
            for (int seat = 0; seat < 4; seat++)
            {
                int ddsHandBase = 4 * SeatMap[seat]; // 4 * converted hand index

                for (int suit = 0; suit < 4; suit++)
                {
                    uint mask = 0u;
                    // iterate ranks Two..Ace by zero-based rank loop
                    for (int r = 0; r < RankMap.Length; r++)
                    {
                        if (deal[(Seats)seat, (Suits)suit, (Ranks)r])
                        {
                            // Use precomputed mask; RankMap[r] gives the bit position
                            mask |= RankMask[RankMap[r] - 2];
                        }
                    }

                    int ddsSuit = SuitMap[suit];
                    dealSpan[ddsHandBase + ddsSuit] = mask;
                }
            }
        }

        public static ddTableDealPBN ToInteropTableDealPbn(string pbn)
        {
            ddTableDealPBN d = default;
            var span = AsSpan(ref d);
            WriteAnsiToSpan(pbn, span);
            return d;
        }

        public static ddTableDeal ToInteropTableDeal(Deal deal)
        {
            ddTableDeal d = default;
            foreach (Seats seat in SeatsExtensions.SeatsAscending)
            {
                var ddsHand = SeatMap[(int)seat];
                foreach (Suits suit in SuitHelper.StandardSuitsAscending)
                {
                    var ddsSuit = SuitMap[(int)suit];
                    uint mask = 0;
                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        if (deal[seat, suit, r])
                            mask |= (2u << ((int)r + 2) - 1);
                    }
                    d.Set(ddsHand, ddsSuit, mask);
                }
            }
            return d;
        }

        //internal static dealPBN ToInteropDealPBN(
        //    Suit trump, Hand leader,
        //    IReadOnlyList<Card> currentTrick, string remaining)
        //{
        //    dealPBN d = default;
        //    d.trump = (int)trump;
        //    d.first = (int)leader;

        //    unsafe
        //    {
        //        for (int i = 0; i < currentTrick.Count && i < 3; i++)
        //        {
        //            d.currentTrickSuit[i] = (int)currentTrick[i].Suit;
        //            d.currentTrickRank[i] = (int)currentTrick[i].Rank;
        //        }


        //        var span = AsSpan(ref d);
        //        WriteAnsiToSpan(remaining, span);
        //    }

        //    return d;
        //}

        internal static deal ToInteropDeal(
            Suit trump, Hand leader,
            PlayedCards played,
            Deal dealRemaining)
        {
            deal d = default;
            d.trump = (int)trump;
            d.first = (int)leader;

            unsafe
            {
                for (int seat = 0; seat <= 3; seat++)
                {
                    var ddsHand = 4 * seat;
                    for (int suit = 0; suit <= 3; suit++)
                    {
                        var ddsSuit = SuitMap[suit];
                        uint mask = 0;
                        for (int rank = 0; rank <= 12; rank++)
                        {
                            if (dealRemaining[seat, suit, rank])
                            {
                                mask |= RankMask[RankMap[rank] - 2];
                            }
                        }

                        d.remainCards[ddsHand + ddsSuit] = mask;
                    }
                }

                d.currentTrickSuit[0] = (int)played.S1;
                d.currentTrickRank[0] = (int)played.R1;
                d.currentTrickSuit[1] = (int)played.S2;
                d.currentTrickRank[1] = (int)played.R2;
                d.currentTrickSuit[2] = (int)played.S3;
                d.currentTrickRank[2] = (int)played.R3;
            }
            return d;
        }

        // ASCII write helper
        private static unsafe void WriteAnsi(
            ReadOnlySpan<char> src, sbyte* dest, int cap)
        {
            int i = 0;
            for (; i < src.Length && i < cap - 1; i++)
                dest[i] = (sbyte)(src[i] <= 0x7F ? src[i] : '?');
            dest[i] = 0;
        }


        public static void WriteAnsiToSpan(ReadOnlySpan<char> src, Span<sbyte> dest)
        {
            int max = dest.Length;
            int i = 0;

            // Copy characters until we reach capacity‑1 (reserve space for NUL)
            for (; i < src.Length && i < max - 1; i++)
            {
                char ch = src[i];
                dest[i] = (sbyte)(ch <= 0x7F ? ch : '?');
            }

            // NUL terminate
            dest[i] = 0;
        }

        public static unsafe Span<sbyte> AsSpan(ref dealPBN d)
        {
            fixed (dealPBN* p = &d)
                return new Span<sbyte>(p->remainCards, 80);
        }

        public static unsafe Span<sbyte> AsSpan(ref ddTableDealPBN d)
        {
            fixed (ddTableDealPBN* p = &d)
                return new Span<sbyte>(p->cards, 80);
        }
    }
}
