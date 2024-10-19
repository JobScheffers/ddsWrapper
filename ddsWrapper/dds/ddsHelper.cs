using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DDS
{
    public struct GameState
    {
        public Deal RemainingCards { get; set; }
        public Suit Trump { get; set; }
        public Hand TrickLeader { get; set; }
        public List<Card> TrickCards { get; set; }

        public GameState() { TrickCards = []; }
    }

    public struct CardPotential
    {
        public Card Card { get; set; }
        public int Tricks { get; set; }
        public override string ToString() => $"{Card.ToString()}:{Tricks.ToString()}";
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
                return data[4 * (int)suit + (int)hand];
            }
            set
            {
                data[4 * (int)suit + (int)hand] = (byte)value;
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

        public Deal(string pbnDeal)
        {
            var hands = pbnDeal.Substring(2).Split(' ');
            var hand = DdsEnum.HandFromPbn(pbnDeal[0]);
            for (int i = 1; i <= 4; i++)
            {
                var suits = hands[i - 1].Split('.');
                for (int pbnSuit = 1; pbnSuit <= 4; pbnSuit++)
                {
                    var suitCards = suits[pbnSuit - 1];
                    var suitLength = suitCards.Length;
                    var suit = DdsEnum.SuitFromPbn(pbnSuit);
                    for (int r = 0; r < suitLength; r++)
                    {
                        var rank = DdsEnum.RankFromPbn(suitCards[r]);
                        this[hand, suit, rank] = true;
                    }
                }

                hand = DdsEnum.NextHandPbn(hand);
            }
        }

        public string ToPBN()
        {
            var localData = this;       // needed because of the delegates
            var result = new StringBuilder(70);
            result.Append("N:");
            DdsEnum.ForEachHand(hand =>
            {
                DdsEnum.ForEachSuitPbn(suit =>
                {
                    DdsEnum.ForEachRankPbn(rank =>
                    {
                        if (localData[hand, suit, rank])
                        {
                            result.Append(DdsEnum.RankToPbn(rank));
                        }
                    });

                    if (suit != Suit.Clubs) result.Append(".");
                });

                if (hand != Hand.West) result.Append(" ");
            });

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
            foreach (Rank rank in Enum.GetValues(typeof(Rank))) toDo(rank);
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

        public static Hand HandFromPbn(Char hand)
        {
            switch (Char.ToUpper(hand))
            {
                case 'N': return Hand.North;
                case 'E': return Hand.East;
                case 'S': return Hand.South;
                case 'W': return Hand.West;
                default: throw new ArgumentOutOfRangeException(nameof(hand), $"unknown {hand}");
            }
        }

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

        public static Rank RankFromPbn(Char rank)
        {
            switch (Char.ToUpper(rank))
            {
                case 'A': return Rank.Ace;
                case 'K': return Rank.King;
                case 'Q': return Rank.Queen;
                case 'J': return Rank.Jack;
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
    }
}
