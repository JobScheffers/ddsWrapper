using Bridge;
using System.Diagnostics;

namespace DDS
{
    [method: DebuggerStepThrough]
    public readonly ref struct GameState(in Deal remainingCards, Suits trump, Seats trickLeader, int playedByMan1, int playedByMan2, int playedByMan3)
    {
        public Deal RemainingCards { get; } = remainingCards;
        public Suits Trump { get; } = trump;
        public Seats TrickLeader { get; } = trickLeader;
        public Bridge.Card PlayedByMan1 { get; } = Bridge.Card.Get(playedByMan1);
        public Bridge.Card PlayedByMan2 { get; } = Bridge.Card.Get(playedByMan2);
        public Bridge.Card PlayedByMan3 { get; } = Bridge.Card.Get(playedByMan3);

        [DebuggerStepThrough]
        public GameState(in Deal remainingCards, Suits trump, Seats trickLeader) : this(in remainingCards, trump, trickLeader, Bridge.Card.Null.Index, Bridge.Card.Null.Index, Bridge.Card.Null.Index) { }
    }

    public readonly struct CardPotential(Bridge.Card card, int tricks, bool isPrimary)
    {
        public int Card { get; } = card.Index; 
        public int Tricks { get; } = tricks; 
        public bool IsPrimary { get; } = isPrimary;

        public override string ToString() => $"{Card}:{Tricks}{(IsPrimary ? " p" : "")}";
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

        internal static DDS.Hand Convert(Seats seat)
        {
            return seat switch
            {
                Seats.North => Hand.North,
                Seats.East => Hand.East,
                Seats.South => Hand.South,
                Seats.West => Hand.West,
                _ => throw new ArgumentOutOfRangeException(nameof(seat), seat.ToString()),
            };
        }

        internal static DDS.Suit Convert(Suits suit)
        {
            return suit switch
            {
                Suits.NoTrump => Suit.NT,
                Suits.Spades => Suit.Spades,
                Suits.Hearts => Suit.Hearts,
                Suits.Diamonds => Suit.Diamonds,
                Suits.Clubs => Suit.Clubs,
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit.ToString()),
            };
        }

        internal static DDS.Rank Convert(Ranks rank)
        {
            return rank switch
            {
                Ranks.Ace => DDS.Rank.Ace,
                Ranks.King => DDS.Rank.King,
                Ranks.Queen => DDS.Rank.Queen,
                Ranks.Jack => DDS.Rank.Jack,
                Ranks.Ten => DDS.Rank.Ten,
                Ranks.Nine => DDS.Rank.Nine,
                Ranks.Eight => DDS.Rank.Eight,
                Ranks.Seven => DDS.Rank.Seven,
                Ranks.Six => DDS.Rank.Six,
                Ranks.Five => DDS.Rank.Five,
                Ranks.Four => DDS.Rank.Four,
                Ranks.Three => DDS.Rank.Three,
                Ranks.Two => DDS.Rank.Two,
                _ => throw new ArgumentOutOfRangeException(nameof(rank), rank.ToString()),
            };
        }

        internal static Seats Convert(DDS.Hand seat)
        {
            return seat switch
            {
                Hand.West => Seats.West,
                Hand.East => Seats.East,
                Hand.North => Seats.North,
                Hand.South => Seats.South,
                _ => throw new ArgumentOutOfRangeException(nameof(seat), seat.ToString()),
            };
        }

        internal static Suits Convert(DDS.Suit suit)
        {
            return suit switch
            {
                Suit.NT => Suits.NoTrump,
                Suit.Spades => Suits.Spades,
                Suit.Hearts => Suits.Hearts,
                Suit.Diamonds => Suits.Diamonds,
                Suit.Clubs => Suits.Clubs,
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit.ToString()),
            };
        }

        internal static Ranks Convert(DDS.Rank rank)
        {
            return rank switch
            {
                DDS.Rank.Ace => Ranks.Ace,
                DDS.Rank.King => Ranks.King,
                DDS.Rank.Queen => Ranks.Queen,
                DDS.Rank.Jack => Ranks.Jack,
                DDS.Rank.Ten => Ranks.Ten,
                DDS.Rank.Nine => Ranks.Nine,
                DDS.Rank.Eight => Ranks.Eight,
                DDS.Rank.Seven => Ranks.Seven,
                DDS.Rank.Six => Ranks.Six,
                DDS.Rank.Five => Ranks.Five,
                DDS.Rank.Four => Ranks.Four,
                DDS.Rank.Three => Ranks.Three,
                DDS.Rank.Two => Ranks.Two,
                _ => throw new ArgumentOutOfRangeException(nameof(rank), rank.ToString()),
            };
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
            return Stopwatch.GetElapsedTime(startTime);
        }
    }
}
