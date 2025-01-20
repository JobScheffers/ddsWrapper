using Bridge;
using System.Diagnostics;

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
        public GameState(in Deal remainingCards, Suits trump, Seats trickLeader) : this(in remainingCards, trump, trickLeader, Bridge.Card.Null, Bridge.Card.Null, Bridge.Card.Null) { }

        [DebuggerStepThrough]
        public GameState(in Deal remainingCards, Suits trump, Seats trickLeader, Bridge.Card playedByMan1, Bridge.Card playedByMan2, Bridge.Card playedByMan3)
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
