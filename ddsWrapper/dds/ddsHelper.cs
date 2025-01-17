using Bridge;
using System.Diagnostics;
using System.Text;

namespace DDS
{
    public readonly ref struct GameState
    {
        public Deal RemainingCards { get; }
        public Suits Trump { get; }
        public Seats TrickLeader { get; }
        public Bridge.Card PlayedByMan1 { get; }
        public Bridge.Card PlayedByMan2 { get; }
        public Bridge.Card PlayedByMan3 { get; }

        [DebuggerStepThrough]
#if NET6_0_OR_GREATER
        public GameState(ref readonly Deal remainingCards, Suits trump, Seats trickLeader) : this(in remainingCards, trump, trickLeader, Bridge.Card.Null, Bridge.Card.Null, Bridge.Card.Null) { }
#else
        public GameState(ref Deal remainingCards, Suits trump, Seats trickLeader) : this(ref remainingCards, trump, trickLeader, Bridge.Card.Null, Bridge.Card.Null, Bridge.Card.Null) { }
#endif

        [DebuggerStepThrough]
#if NET6_0_OR_GREATER
        public GameState(ref readonly Deal remainingCards, Suits trump, Seats trickLeader, Bridge.Card playedByMan1, Bridge.Card playedByMan2, Bridge.Card playedByMan3)
#else
        public GameState(ref Deal remainingCards, Suits trump, Seats trickLeader, Bridge.Card playedByMan1, Bridge.Card playedByMan2, Bridge.Card playedByMan3)
#endif
        {
            RemainingCards = remainingCards;
            Trump = trump;
            TrickLeader = trickLeader;
            PlayedByMan1 = playedByMan1;
            PlayedByMan2 = playedByMan2;
            PlayedByMan3 = playedByMan3;
            //Debug.WriteLine(RemainingCards.ToPBN());
        }
    }

    public readonly struct CardPotential
    {
        public Bridge.Card Card { get; }
        public int Tricks { get; }
        public bool IsPrimary { get; }

        public CardPotential(Bridge.Card card, int tricks, bool isPrimary) { Card = card; Tricks = tricks; IsPrimary = isPrimary; }
        public override string ToString() => $"{Card.ToString()}:{Tricks.ToString()}{(IsPrimary ? " p" : "")}";
    }

    public unsafe struct TableResults
    {
        private fixed byte data[20];

        public int this[Seats hand, Suits suit]
        {
            get
            {
                return this[(int)hand, (int)suit];
            }
            set
            {
                this[(int)hand, (int)suit] = (byte)value;
            }
        }

        public int this[int hand, int suit]
        {
            get
            {
                return data[4 * suit + hand];
            }
            set
            {
                data[4 * suit + hand] = (byte)value;
            }
        }
    }

    public unsafe struct Deal
    {
        private fixed ushort data[13];

        public bool this[Seats seat, Suits suit, Ranks rank]
        {
            get
            {
                return this[(int)seat, (int)suit, (int)rank];
            }
            set
            {
                this[(int)seat, (int)suit, (int)rank] = value;
            }
        }

        private unsafe bool this[int seat, int suit, int rank]
        {
            get
            {
                //Debug.WriteLine($"{((Seats)seat).ToXML()}{((Suits)suit).ToXML()}{((Ranks)rank).ToXML()}? {Convert.ToString(data[rank], 2)} {Convert.ToString((1 << (4 * seat + suit)), 2)}");
                return (data[rank] & (1 << (4 * seat + suit))) > 0;
            }
            set
            {
                //Debug.WriteLine($"{((Seats)seat).ToXML()}{((Suits)suit).ToXML()}{((Ranks)rank).ToXML()}={value} {Convert.ToString(data[rank], 2)} {Convert.ToString((1 << (4 * seat + suit)), 2)}");
                if (value)
                {
                    data[rank] |= (ushort)(1 << (4 * seat + suit));
                }
                else
                {
                    data[rank] &= (ushort)(ushort.MaxValue - (1 << (4 * seat + suit)));
                }
                //Debug.WriteLine($"{Convert.ToString(data[rank], 2)} {Convert.ToString((1 << (4 * seat + suit)), 2)}");
            }
        }

        //[DebuggerStepThrough]
#if NET6_0_OR_GREATER
        public Deal(ref readonly string pbnDeal)
#else
        public Deal(string pbnDeal)
#endif
        {
            var firstHand = pbnDeal[0];
#if NET6_0_OR_GREATER
            var hands = pbnDeal[2..].Split2(' ');
#else
            var hands = pbnDeal.Substring(2).Split(' ');
#endif
            var hand = DdsEnum.HandFromPbn(in firstHand);
            foreach (var handHolding in hands)
            {
#if NET6_0_OR_GREATER
                var suits = handHolding.Line.Split2('.');
#else
                var suits = handHolding.Split('.');
#endif
                int pbnSuit = 1;
                foreach (var suitHolding in suits)
                {
#if NET6_0_OR_GREATER
                    var suitCards = suitHolding.Line;
#else
                    var suitCards = suitHolding;
#endif
                    var suitLength = suitCards.Length;
                    var suit = DdsEnum.SuitFromPbn(pbnSuit);
                    for (int r = 0; r < suitLength; r++)
                    {
#if NET6_0_OR_GREATER
                        var rank = DdsEnum.RankFromPbn(in suitCards[r]);
#else
                        var x = suitCards[r];
                        var rank = DdsEnum.RankFromPbn(in x);
#endif
                        this[hand, suit, rank] = true;
                    }
                    pbnSuit++;
                }

                hand = DdsEnum.NextHandPbn(hand);
            }
        }

        public string ToPBN()
        {
            var result = new StringBuilder(70);
            result.Append("N:");
            for (Seats hand = Seats.North; hand <= Seats.West; hand++)
            {
                for (Suits suit = Suits.Spades; suit >= Suits.Clubs; suit--)
                {
                    for (Ranks rank = Ranks.Ace; rank >= Ranks.Two; rank--)
                    {
                        if (this[hand, suit, rank])
                        {
                            result.Append(DdsEnum.RankToPbn(rank));
                        }
                    };

                    if (suit != Suits.Clubs) result.Append(".");
                };

                if (hand != Seats.West) result.Append(" ");
            };

            return result.ToString();
        }
    }

    public static class DdsEnum
    {
        /// <summary>
        /// action called for Clubs, Diamonds, Hearts, Spades, NoTrump
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachTrump(Action<Suits> toDo)
        {
            toDo(Suits.Clubs);
            toDo(Suits.Diamonds);
            toDo(Suits.Hearts);
            toDo(Suits.Spades);
            toDo(Suits.NoTrump);
        }

        /// <summary>
        /// action called for Clubs, Diamonds, Hearts, Spades
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachSuit(Action<Suits> toDo)
        {
            toDo(Suits.Clubs);
            toDo(Suits.Diamonds);
            toDo(Suits.Hearts);
            toDo(Suits.Spades);
        }

        /// <summary>
        /// action called for Spades, Hearts, Diamonds, Clubs (pbn required suit order)
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachSuitPbn(Action<Suits> toDo)
        {
            toDo(Suits.Spades);
            toDo(Suits.Hearts);
            toDo(Suits.Diamonds);
            toDo(Suits.Clubs);
        }

        /// <summary>
        /// action called for North, East, South, West
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachHand(Action<Seats> toDo)
        {
            toDo(Seats.North);
            toDo(Seats.East);
            toDo(Seats.South);
            toDo(Seats.West);
        }

        /// <summary>
        /// action called ....
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachRank(Action<Ranks> toDo)
        {
            //foreach (Ranks rank in Enum.GetValues(typeof(Ranks))) toDo(rank);
            ForEachRankPbn(toDo);
        }

        /// <summary>
        /// action called ....
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachRankPbn(Action<Ranks> toDo)
        {
            toDo(Ranks.Ace);
            toDo(Ranks.King);
            toDo(Ranks.Queen);
            toDo(Ranks.Jack);
            toDo(Ranks.Ten);
            toDo(Ranks.Nine);
            toDo(Ranks.Eight);
            toDo(Ranks.Seven);
            toDo(Ranks.Six);
            toDo(Ranks.Five);
            toDo(Ranks.Four);
            toDo(Ranks.Three);
            toDo(Ranks.Two);
        }

        [DebuggerStepThrough]
        public static Seats HandFromPbn(ref readonly Char hand)
        {
            switch (hand)
            {
                case 'n':
                case 'N': return Seats.North;
                case 'e':
                case 'E': return Seats.East;
                case 's':
                case 'S': return Seats.South;
                case 'w':
                case 'W': return Seats.West;
                default: throw new ArgumentOutOfRangeException(nameof(hand), $"unknown {hand}");
            }
        }

        [DebuggerStepThrough]
        public static Seats NextHandPbn(Seats hand)
        {
            switch (hand)
            {
                case Seats.North: return Seats.East;
                case Seats.East: return Seats.South;
                case Seats.South: return Seats.West;
                case Seats.West: return Seats.North;
                default: throw new ArgumentOutOfRangeException(nameof(hand), $"unknown {hand}");
            }
        }

        [DebuggerStepThrough]
        public static Suits SuitFromPbn(int relativeSuit)
        {
            switch (relativeSuit)
            {
                case 1: return Suits.Spades;
                case 2: return Suits.Hearts;
                case 3: return Suits.Diamonds;
                case 4: return Suits.Clubs;
                default: throw new ArgumentOutOfRangeException(nameof(relativeSuit), $"unknown {relativeSuit}");
            }
        }

        [DebuggerStepThrough]
        public static Ranks RankFromPbn(ref readonly Char rank)
        {
            switch (rank)
            {
                case 'a':
                case 'A': return Ranks.Ace;
                case 'k':
                case 'h':
                case 'H':
                case 'K': return Ranks.King;
                case 'q':
                case 'Q': return Ranks.Queen;
                case 'j':
                case 'b':
                case 'B':
                case 'J': return Ranks.Jack;
                case 't':
                case 'T': return Ranks.Ten;
                case '9': return Ranks.Nine;
                case '8': return Ranks.Eight;
                case '7': return Ranks.Seven;
                case '6': return Ranks.Six;
                case '5': return Ranks.Five;
                case '4': return Ranks.Four;
                case '3': return Ranks.Three;
                case '2': return Ranks.Two;
                default: throw new ArgumentOutOfRangeException(nameof(rank), $"unknown {rank}");
            }
        }

        public static string RankToPbn(Ranks rank)
        {
            switch (rank)
            {
                case Ranks.Ace: return "A";
                case Ranks.King: return "K";
                case Ranks.Queen: return "Q";
                case Ranks.Jack: return "J";
                case Ranks.Ten: return "T";
                case Ranks.Nine: return "9";
                case Ranks.Eight: return "8";
                case Ranks.Seven: return "7";
                case Ranks.Six: return "6";
                case Ranks.Five: return "5";
                case Ranks.Four: return "4";
                case Ranks.Three: return "3";
                case Ranks.Two: return "2";
                default: throw new ArgumentOutOfRangeException(nameof(rank), $"unknown {rank}");
            }
        }

        public static DDS.Hand Convert(Seats seat)
        {
            switch (seat)
            {
                case Seats.North: return Hand.North;
                case Seats.East: return Hand.East;
                case Seats.South: return Hand.South;
                case Seats.West: return Hand.West;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public static DDS.Suit Convert(Suits suit)
        {
            switch (suit)
            {
                case Suits.NoTrump: return Suit.NT;
                case Suits.Spades: return Suit.Spades;
                case Suits.Hearts: return Suit.Hearts;
                case Suits.Diamonds: return Suit.Diamonds;
                case Suits.Clubs: return Suit.Clubs;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public static DDS.Rank Convert(Ranks rank)
        {
            switch (rank)
            {
                case Ranks.Ace: return DDS.Rank.Ace;
                case Ranks.King: return DDS.Rank.King;
                case Ranks.Queen: return DDS.Rank.Queen;
                case Ranks.Jack: return DDS.Rank.Jack;
                case Ranks.Ten: return DDS.Rank.Ten;
                case Ranks.Nine: return DDS.Rank.Nine;
                case Ranks.Eight: return DDS.Rank.Eight;
                case Ranks.Seven: return DDS.Rank.Seven;
                case Ranks.Six: return DDS.Rank.Six;
                case Ranks.Five: return DDS.Rank.Five;
                case Ranks.Four: return DDS.Rank.Four;
                case Ranks.Three: return DDS.Rank.Three;
                case Ranks.Two: return DDS.Rank.Two;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public static Seats Convert(DDS.Hand seat)
        {
            switch (seat)
            {
                case Hand.West: return Seats.West;
                case Hand.East: return Seats.East;
                case Hand.North: return Seats.North;
                case Hand.South: return Seats.South;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public static Suits Convert(DDS.Suit suit)
        {
            switch (suit)
            {
                case Suit.NT: return Suits.NoTrump;
                case Suit.Spades: return Suits.Spades;
                case Suit.Hearts: return Suits.Hearts;
                case Suit.Diamonds: return Suits.Diamonds;
                case Suit.Clubs: return Suits.Clubs;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public static Ranks Convert(DDS.Rank rank)
        {
            switch (rank)
            {
                case DDS.Rank.Ace: return Ranks.Ace;
                case DDS.Rank.King: return Ranks.King;
                case DDS.Rank.Queen: return Ranks.Queen;
                case DDS.Rank.Jack: return Ranks.Jack;
                case DDS.Rank.Ten: return Ranks.Ten;
                case DDS.Rank.Nine: return Ranks.Nine;
                case DDS.Rank.Eight: return Ranks.Eight;
                case DDS.Rank.Seven: return Ranks.Seven;
                case DDS.Rank.Six: return Ranks.Six;
                case DDS.Rank.Five: return Ranks.Five;
                case DDS.Rank.Four: return Ranks.Four;
                case DDS.Rank.Three: return Ranks.Three;
                case DDS.Rank.Two: return Ranks.Two;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        internal static PlayedCards Convert(Bridge.Card card1, Bridge.Card card2, Bridge.Card card3)
        {
            return new(
                Bridge.Card.IsNull(card1) ? 0 : Convert(card1.Suit),
                Bridge.Card.IsNull(card1) ? 0 : Convert(card1.Rank),
                Bridge.Card.IsNull(card2) ? 0 : Convert(card2.Suit),
                Bridge.Card.IsNull(card2) ? 0 : Convert(card2.Rank),
                Bridge.Card.IsNull(card3) ? 0 : Convert(card3.Suit),
                Bridge.Card.IsNull(card3) ? 0 : Convert(card3.Rank)
            );
        }

#if NET6_0_OR_GREATER
        public static LineSplitEnumerator Split2(this string str, char splitter)
        {
            // LineSplitEnumerator is a struct so there is no allocation here
            return new LineSplitEnumerator(str.AsSpan(), splitter);
        }

        public static LineSplitEnumerator Split2(this ReadOnlySpan<char> str, char splitter)
        {
            // LineSplitEnumerator is a struct so there is no allocation here
            return new LineSplitEnumerator(str, splitter);
        }

        // Must be a ref struct as it contains a ReadOnlySpan<char>
        public ref struct LineSplitEnumerator
        {
            private ReadOnlySpan<char> _str;
            private readonly char _splitter;

            public LineSplitEnumerator(ReadOnlySpan<char> str, char splitter)
            {
                _str = str;
                _splitter = splitter;
                Current = default;
            }

            // Needed to be compatible with the foreach operator
            public LineSplitEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                var span = _str;
                if (span.Length == 0) // Reach the end of the string
                    return false;

                var index = span.IndexOf(_splitter);
                if (index == -1) // The string is composed of only one line
                {
                    _str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
                    Current = new LineSplitEntry(span, ReadOnlySpan<char>.Empty);
                    return true;
                }

                Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 1));
                _str = span.Slice(index + 1);
                return true;
            }

            public LineSplitEntry Current { get; private set; }
        }

        public readonly ref struct LineSplitEntry
        {
            public LineSplitEntry(ReadOnlySpan<char> line, ReadOnlySpan<char> separator)
            {
                Line = line;
                Separator = separator;
            }

            public ReadOnlySpan<char> Line { get; }
            public ReadOnlySpan<char> Separator { get; }

            // This method allow to deconstruct the type, so you can write any of the following code
            // foreach (var entry in str.SplitLines()) { _ = entry.Line; }
            // foreach (var (line, endOfLine) in str.SplitLines()) { _ = line; }
            // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/deconstruct?WT.mc_id=DT-MVP-5003978#deconstructing-user-defined-types
            public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
            {
                line = Line;
                separator = Separator;
            }

            // This method allow to implicitly cast the type into a ReadOnlySpan<char>, so you can write the following code
            // foreach (ReadOnlySpan<char> entry in str.SplitLines())
            public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry) => entry.Line;
        }
#endif
    }

    public static class Profiler
    {
        public static T Time<T>(Func<T> toDo, out TimeSpan elapsedTime, int repetitions = 1)
        {
            var startTime = GetStartTime();
            var result = toDo();
            for (int i = 1; i < repetitions; i++) toDo();
            elapsedTime = GetElapsedTime(startTime);
            return result;
        }

        public static T DebugTime<T>(Func<T> toDo, out TimeSpan elapsedTime, int repetitions = 1)
        {
#if !DEBUG
            elapsedTime = TimeSpan.FromTicks(0);
#endif
            return
#if DEBUG
                Time(() =>
                {
                    return
#endif
                        toDo();
#if DEBUG
                }, out elapsedTime, repetitions);
#endif
        }

        public static void Time(Action toDo, out TimeSpan elapsedTime, int repetitions = 1)
        {
            Time<int>(() =>
            {
                toDo();
                return 0;
            }, out elapsedTime, repetitions);
        }

        /// <summary>
        /// Only when in DEBUG mode
        /// </summary>
        /// <param name="toDo"></param>
        /// <param name="elapsedTime"></param>
        /// <param name="repetitions"></param>
        public static void DebugTime(Action toDo, out TimeSpan elapsedTime, int repetitions = 1)
        {
#if DEBUG
            Time(() =>
            {
#else
            elapsedTime = TimeSpan.FromTicks(0);
#endif
            toDo();
#if DEBUG
            }, out elapsedTime, repetitions);
#endif
        }

        public static long GetStartTime()
        {
            return Stopwatch.GetTimestamp();
        }

        public static TimeSpan GetElapsedTime(long startTime)
        {
#if NET7_0_OR_GREATER
            return Stopwatch.GetElapsedTime(startTime);
#else
            var stopTime = Stopwatch.GetTimestamp();
            return TimeSpan.FromTicks(stopTime - startTime);
#endif
        }
    }
}
