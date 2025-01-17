using Bridge;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DDS
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly ref struct parResultsDealer
    {
        public readonly int number;
        public readonly int score;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public readonly string contracts;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ddTableResults
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        private readonly int[] resTable;

        public ddTableResults()
        {
            resTable = new int[20];
        }

        public int this[Hand hand, Suit suit]
        {
            get { return this[(int)hand, (int)suit]; }
        }

        public int this[int hand, int suit]
        {
            get { return resTable[4 * suit + hand]; }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ddTableDealPBN
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public readonly string cards;

        public ddTableDealPBN(string hands)
        {
            cards = hands;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ddTableDealsPBN
    {
        public readonly int noOfTables;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains)]
        public readonly ddTableDealPBN[] deals;

        public ddTableDealsPBN(ref readonly List<string> hands)
        {
            noOfTables = hands.Count;
            deals = new ddTableDealPBN[ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains];
            for (int hand = 0; hand < hands.Count; hand++) deals[hand] = new ddTableDealPBN(hands[hand]);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ddTableDeal
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly uint[,] cards;

        public ddTableDeal(ref readonly Deal deal)
        {
            cards = new uint[4, 4];
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
                    {
                        if (deal[seat, suit, rank])
                        {
                            cards[(int)DdsEnum.Convert(seat), (int)DdsEnum.Convert(suit)] |= (uint)(2 << ((int)rank + 2) - 1);
                        }
                    }
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ddTableDeals
    {
        public readonly int noOfTables;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains)]
        public readonly ddTableDeal[] tableDeals;

        public ddTableDeals(ref readonly List<Deal> deals)
        {
            noOfTables = deals.Count;
            tableDeals = new ddTableDeal[ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains];
            for (int hand = 0; hand < deals.Count; hand++) tableDeals[hand] = new ddTableDeal(deals[hand]);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ddTablesResult
    {
        public readonly int noOfBoards;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains)]
        public readonly ddTableResults[] results;

        public ddTablesResult(int deals)
        {
            noOfBoards = deals;
            results = new ddTableResults[ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains];
            for (int deal = 0; deal < deals; deal++) results[deal] = new ddTableResults();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct parResults
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly parScore[] parScores;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly parContract[] parContracts;

        public parResults()
        {
            parScores = new parScore[2];
            parContracts = new parContract[2];
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct parScore
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public readonly string score;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct parContract
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public readonly string score;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct allParResults
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public readonly parResults[] results;

        public allParResults()
        {
            results = new parResults[20];
            for (int i = 0; i < 20; i++) results[i] = new parResults();
        }
    }

    internal readonly struct dealPBN
    {
        public readonly int trump;

        public readonly int first;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly int[] currentTrickSuit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly int[] currentTrickRank;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public readonly string remainCards;

        public dealPBN(Suit _trump, Hand trickLeader, ref readonly Card[] currentTrick, ref readonly string remainingCards)
        {
            trump = (int)_trump;
            first = (int)trickLeader;
            remainCards = remainingCards;
            currentTrickSuit = new int[3];
            currentTrickRank = new int[3];
            for (int i = 0; i < currentTrick.Length; i++)
            {
                currentTrickSuit[i] = (int)currentTrick[i].Suit;
                currentTrickRank[i] = (int)currentTrick[i].Rank;
            }
        }
    }

#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    internal readonly struct deal
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    {
        public readonly int trump;

        public readonly int first;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly int[] currentTrickSuit = new int[3];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly int[] currentTrickRank = new int[3];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly uint[,] remainCards;

        public deal(Suit _trump, Hand trickLeader, ref readonly PlayedCards playedCards, ref readonly Deal remainingCards)
        {
            //Debug.WriteLine(remainingCards.ToPBN());
            trump = (int)_trump;
            first = (int)trickLeader;
            remainCards = new uint[4,4];
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
                    {
                        if (remainingCards[seat, suit, rank])
                        {
                            remainCards[(int)DdsEnum.Convert(seat), (int)DdsEnum.Convert(suit)] |= (uint)(2 << ((int)DdsEnum.Convert(rank)) - 1);
                        }
                    }
                }
            }

            currentTrickSuit[0] = (int)playedCards.Card1Suit;
            currentTrickRank[0] = (int)playedCards.Card1Rank;
            currentTrickSuit[1] = (int)playedCards.Card2Suit;
            currentTrickRank[1] = (int)playedCards.Card2Rank;
            currentTrickSuit[2] = (int)playedCards.Card3Suit;
            currentTrickRank[2] = (int)playedCards.Card3Rank;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DDSInfo
    {
        public readonly int major, minor, patch;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public readonly string versionString;

        // Currently 0 = unknown, 1 = Windows, 2 = Cygwin, 3 = Linux, 4 = Apple
        public readonly int system;

        // We know 32 and 64-bit systems.
        public readonly int numBits;

        // Currently 0 = unknown, 1 = Microsoft Visual C++, 2 = mingw,
        // 3 = GNU g++, 4 = clang
        public readonly int compiler;

        // Currently 0 = none, 1 = DllMain, 2 = Unix-style
        public readonly int constructor;

        public readonly int numCores;

        // Currently 
        // 0 = none, 
        // 1 = Windows (native), 
        // 2 = OpenMP, 
        // 3 = GCD,
        // 4 = Boost,
        // 5 = STL,
        // 6 = TBB,
        // 7 = STLIMPL (for_each), experimental only
        // 8 = PPLIMPL (for_each), experimental only
        public readonly int threading;

        // The actual number of threads configured
        public readonly int noOfThreads;

        // This will break if there are > 128 threads...
        // The string is of the form LLLSSS meaning 3 large TT memories
        // and 3 small ones.
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public readonly string threadSizes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public readonly string systemString;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly ref struct FutureTricks
    {
        /// <summary>
        /// /* Number of searched nodes */
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public readonly int nodes;

        /// <summary>
        /// /*  No of alternative cards  */
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public readonly int cards;

        /// <summary>
        /// /* 0=Spades, 1=Hearts, 2=Diamonds, 3=Clubs */
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public readonly int[] suit;

        /// <summary>
        /// /* 2-14 for 2 through Ace */ 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public readonly int[] rank;

        /// <summary>
        /// /* Bit string of ranks for equivalent lower rank cards. The decimal value range between 4 (=2) and 8192 (King=rank 13). 
        ///  When there are several ”equals”, the value is the sum of each ”equal”. */
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public readonly int[] equals;

        /// <summary>
        /// /* -1 indicates that target was not reached, otherwise target or max numbe of tricks */ 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public readonly int[] score;
    }

    public enum Suit { Spades = 0, Hearts = 1, Diamonds = 2, Clubs = 3, NT = 4}

    public enum Rank { Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10, Jack = 11, Queen = 12, King = 13, Ace = 14 }

    public enum Hand { North = 0, East = 1, South = 2, West = 3 }

    public enum Vulnerable { None = 0, Both = 1, NSonly = 2, EWonly = 3 }

    internal readonly struct Card
    {
        public Suit Suit { get; }
        public Rank Rank { get; }

        public Card(Suit s, Rank r) { Suit = s; Rank = r; }
        public override string ToString() => $"{Suit.ToString()[0]}{(Rank < Rank.Ten ? ((int)Rank).ToString() : Rank.ToString()[0])}";
    }

    internal readonly ref struct PlayedCards
    {
        public Suit Card1Suit { get; }
        public Suit Card2Suit { get; }
        public Suit Card3Suit { get; }
        public Rank Card1Rank { get; }
        public Rank Card2Rank { get; }
        public Rank Card3Rank { get; }

        public PlayedCards(Suit s1, Rank r1, Suit s2, Rank r2, Suit s3, Rank r3)
        {
            Card1Suit = s1;
            Card1Rank = r1;
            Card2Suit = s2;
            Card2Rank = r2;
            Card3Suit = s3;
            Card3Rank = r3;
        }
    }
}
