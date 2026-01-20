
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
        public static ddTableDealPBN ToInteropTableDealPbn(string pbn)
        {
            ddTableDealPBN d = default;
            var span = AsSpan(ref d);
            WriteAnsiToSpan(pbn, span);
            //fixed (sbyte* p = d.cards)
            //  WriteAnsi(pbn, p, 80);
            return d;
        }

        public static ddTableDeal ToInteropTableDeal(Deal deal)
        {
            ddTableDeal d = default;
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                var ddsHand = (int)DdsEnum.Convert(seat);
                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    var ddsSuit = (int)DdsEnum.Convert(suit);
                    uint mask = 0;
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        if (deal[seat, suit, r])
                            mask |= (uint)(2 << ((int)r + 2) - 1);
                    }
                    d.Set(ddsHand, ddsSuit, mask);
                }
            }
            return d;
        }

        public static unsafe ddTableDeals ToInteropTableDeals(in List<Deal> deals)
        {
            int count = deals.Count;
            if (count > ddsImports.ddsMaxNumberOfBoards)
                throw new ArgumentOutOfRangeException(nameof(deals),
                    $"Cannot exceed {ddsImports.ddsMaxNumberOfBoards} deals.");

            ddTableDeals tableDeals = default;
            tableDeals.noOfTables = count;

            for (int dealIndex = 0; dealIndex < count; dealIndex++)
            {
                // Get the span for this deal (16 uints)
                Span<uint> dealSpan = tableDeals[dealIndex];
                for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                {
                    var ddsHand = (int)DdsEnum.Convert(seat);
                    for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                    {
                        var ddsSuit = (int)DdsEnum.Convert(suit);
                        uint mask = 0;
                        for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                        {
                            if (deals[dealIndex][seat, suit, r])
                                mask |= (uint)(2 << ((int)DdsEnum.Convert(r)) - 1);
                        }

                        dealSpan[ddsHand * 4 + ddsSuit] = mask;
                    }
                }
            }

            return tableDeals;
        }

        public static unsafe ddTableDeals ToInteropTableDeals(in Deal[] deals)
        {
            int count = deals.Length;
            if (count > ddsImports.ddsMaxNumberOfBoards)
                throw new ArgumentOutOfRangeException(nameof(deals),
                    $"Cannot exceed {ddsImports.ddsMaxNumberOfBoards} deals.");

            ddTableDeals tableDeals = default;
            tableDeals.noOfTables = count;
            for (int dealIndex = 0; dealIndex < count; dealIndex++)
            {
                int cards = 0;
                // Get the span for this deal (16 uints)
                Span<uint> dealSpan = tableDeals[dealIndex];
                for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                {
                    var ddsHand = (int)DdsEnum.Convert(seat);
                    for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                    {
                        var ddsSuit = (int)DdsEnum.Convert(suit);
                        uint mask = 0;
                        for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                        {
                            if (deals[dealIndex][seat, suit, r])
                            {
                                mask |= (uint)(2 << ((int)DdsEnum.Convert(r)) - 1);
                                cards++;
                            }
                        }

                        dealSpan[ddsHand * 4 + ddsSuit] = mask;
                    }
                }
            }

            return tableDeals;
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
                int cards = 0;
                for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                {
                    var ddsHand = (int)DdsEnum.Convert(seat);
                    for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                    {
                        var ddsSuit = (int)DdsEnum.Convert(suit);
                        uint mask = 0;
                        for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                        {
                            if (dealRemaining[seat, suit, r])
                            {
                                mask |= (uint)(2 << ((int)DdsEnum.Convert(r)) - 1);
                                cards++;
                            }
                        }

                        d.remainCards[ddsHand * 4 + ddsSuit] = mask;
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
