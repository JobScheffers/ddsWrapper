using System.Diagnostics;
using System.Text;

namespace DDS
{
    public readonly ref struct GameState
    {
        public Deal RemainingCards { get; }
        public Suit Trump { get; }
        public Hand TrickLeader { get; }
        public List<Card> TrickCards { get; }

        [DebuggerStepThrough]
        public GameState(ref readonly Deal remainingCards, Suit trump, Hand trickLeader) : this(in remainingCards, trump, trickLeader, []) { }

        [DebuggerStepThrough]
        public GameState(ref readonly Deal remainingCards, Suit trump, Hand trickLeader, List<Card> trickCards)
        {
            RemainingCards = remainingCards;
            Trump = trump;
            TrickLeader = trickLeader;
            TrickCards = trickCards;
        }
    }

    public readonly struct CardPotential
    {
        public Card Card { get; }
        public int Tricks { get; }
        public bool IsPrimary { get; }

        public CardPotential(Card card, int tricks, bool isPrimary) { Card = card; Tricks = tricks; IsPrimary = isPrimary; }
        public override string ToString() => $"{Card.ToString()}:{Tricks.ToString()}{(IsPrimary ? " p" : "")}";
    }

    public struct SuitCollection<T>
    {
        private T[] data;

        public SuitCollection(T[] _data)
        {
            data = _data;
        }

        public T this[Suit suit]
        {
            get
            {
                return data[(int)suit];
            }
            set
            {
                data[(int)suit] = value;
            }
        }
    }

    public unsafe struct TableResults
    {
        private fixed byte data[20];

        public int this[Hand hand, Suit suit]
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

        public bool this[Hand seat, Suit suit, Rank rank]
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

        public unsafe bool this[int seat, int suit, int rank]
        {
            get
            {
                //Debug.WriteLine($"{((Seats)seat).ToXML()}{((Suits)suit).ToXML()}{((Ranks)rank).ToXML()}? {Convert.ToString(data[rank], 2)} {Convert.ToString((1 << (4 * seat + suit)), 2)}");
                return (data[rank - 2] & (1 << (4 * seat + suit))) > 0;
            }
            set
            {
                //Debug.WriteLine($"{((Seats)seat).ToXML()}{((Suits)suit).ToXML()}{((Ranks)rank).ToXML()}={value} {Convert.ToString(data[rank], 2)} {Convert.ToString((1 << (4 * seat + suit)), 2)}");
                if (value)
                {
                    data[rank - 2] |= (ushort)(1 << (4 * seat + suit));
                }
                else
                {
                    data[rank - 2] &= (ushort)(ushort.MaxValue - (1 << (4 * seat + suit)));
                }
                //Debug.WriteLine($"{Convert.ToString(data[rank], 2)} {Convert.ToString((1 << (4 * seat + suit)), 2)}");
            }
        }

        [DebuggerStepThrough]
        public Deal(ref readonly string pbnDeal)
        {
            var firstHand = pbnDeal[0];
            var hands = pbnDeal[2..].Split2(' ');
            var hand = DdsEnum.HandFromPbn(in firstHand);
            foreach (var handHolding in hands)
            {
                var suits = handHolding.Line.Split2('.');
                int pbnSuit = 1;
                foreach (var suitHolding in suits)
                {
                    var suitCards = suitHolding.Line;
                    var suitLength = suitCards.Length;
                    var suit = DdsEnum.SuitFromPbn(pbnSuit);
                    for (int r = 0; r < suitLength; r++)
                    {
                        var rank = DdsEnum.RankFromPbn(in suitCards[r]);
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
            for (Hand hand = Hand.North; hand <= Hand.West; hand++)
            {
                for (Suit suit = Suit.Spades; suit <= Suit.Clubs; suit++)
                {
                    for (Rank rank = Rank.Ace; rank >= Rank.Two; rank--)
                    {
                        if (this[hand, suit, rank])
                        {
                            result.Append(DdsEnum.RankToPbn(rank));
                        }
                    };

                    if (suit != Suit.Clubs) result.Append(".");
                };

                if (hand != Hand.West) result.Append(" ");
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
        public static void ForEachTrump(Action<Suit> toDo)
        {
            toDo(Suit.Clubs);
            toDo(Suit.Diamonds);
            toDo(Suit.Hearts);
            toDo(Suit.Spades);
            toDo(Suit.NT);
        }

        /// <summary>
        /// action called for Clubs, Diamonds, Hearts, Spades
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachSuit(Action<Suit> toDo)
        {
            toDo(Suit.Clubs);
            toDo(Suit.Diamonds);
            toDo(Suit.Hearts);
            toDo(Suit.Spades);
        }

        /// <summary>
        /// action called for Spades, Hearts, Diamonds, Clubs (pbn required suit order)
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachSuitPbn(Action<Suit> toDo)
        {
            toDo(Suit.Spades);
            toDo(Suit.Hearts);
            toDo(Suit.Diamonds);
            toDo(Suit.Clubs);
        }

        /// <summary>
        /// action called for North, East, South, West
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachHand(Action<Hand> toDo)
        {
            toDo(Hand.North);
            toDo(Hand.East);
            toDo(Hand.South);
            toDo(Hand.West);
        }

        /// <summary>
        /// action called ....
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachRank(Action<Rank> toDo)
        {
            //foreach (Rank rank in Enum.GetValues(typeof(Rank))) toDo(rank);
            ForEachRankPbn(toDo);
        }

        /// <summary>
        /// action called ....
        /// no knowledge needed of the int values of this enum
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachRankPbn(Action<Rank> toDo)
        {
            toDo(Rank.Ace);
            toDo(Rank.King);
            toDo(Rank.Queen);
            toDo(Rank.Jack);
            toDo(Rank.Ten);
            toDo(Rank.Nine);
            toDo(Rank.Eight);
            toDo(Rank.Seven);
            toDo(Rank.Six);
            toDo(Rank.Five);
            toDo(Rank.Four);
            toDo(Rank.Three);
            toDo(Rank.Two);
        }

        [DebuggerStepThrough]
        public static Hand HandFromPbn(ref readonly Char hand)
        {
            switch (hand)
            {
                case 'n':
                case 'N': return Hand.North;
                case 'e':
                case 'E': return Hand.East;
                case 's':
                case 'S': return Hand.South;
                case 'w':
                case 'W': return Hand.West;
                default: throw new ArgumentOutOfRangeException(nameof(hand), $"unknown {hand}");
            }
        }

        [DebuggerStepThrough]
        public static Hand NextHandPbn(Hand hand)
        {
            switch (hand)
            {
                case Hand.North: return Hand.East;
                case Hand.East: return Hand.South;
                case Hand.South: return Hand.West;
                case Hand.West: return Hand.North;
                default: throw new ArgumentOutOfRangeException(nameof(hand), $"unknown {hand}");
            }
        }

        [DebuggerStepThrough]
        public static Suit SuitFromPbn(int relativeSuit)
        {
            switch (relativeSuit)
            {
                case 1: return Suit.Spades;
                case 2: return Suit.Hearts;
                case 3: return Suit.Diamonds;
                case 4: return Suit.Clubs;
                default: throw new ArgumentOutOfRangeException(nameof(relativeSuit), $"unknown {relativeSuit}");
            }
        }

        [DebuggerStepThrough]
        public static Rank RankFromPbn(ref readonly Char rank)
        {
            switch (rank)
            {
                case 'a':
                case 'A': return Rank.Ace;
                case 'k':
                case 'h':
                case 'H':
                case 'K': return Rank.King;
                case 'q':
                case 'Q': return Rank.Queen;
                case 'j':
                case 'b':
                case 'B':
                case 'J': return Rank.Jack;
                case 't':
                case 'T': return Rank.Ten;
                case '9': return Rank.Nine;
                case '8': return Rank.Eight;
                case '7': return Rank.Seven;
                case '6': return Rank.Six;
                case '5': return Rank.Five;
                case '4': return Rank.Four;
                case '3': return Rank.Three;
                case '2': return Rank.Two;
                default: throw new ArgumentOutOfRangeException(nameof(rank), $"unknown {rank}");
            }
        }

        public static string RankToPbn(Rank rank)
        {
            switch (rank)
            {
                case Rank.Ace: return "A";
                case Rank.King: return "K";
                case Rank.Queen: return "Q";
                case Rank.Jack: return "J";
                case Rank.Ten: return "T";
                case Rank.Nine: return "9";
                case Rank.Eight: return "8";
                case Rank.Seven: return "7";
                case Rank.Six: return "6";
                case Rank.Five: return "5";
                case Rank.Four: return "4";
                case Rank.Three: return "3";
                case Rank.Two: return "2";
                default: throw new ArgumentOutOfRangeException(nameof(rank), $"unknown {rank}");
            }
        }

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
