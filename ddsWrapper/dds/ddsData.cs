using System.Runtime.InteropServices;

namespace DDS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct parResultsDealer
    {
        public int number;
        public int score;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string contracts;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ddTableResults
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        private int[] resTable;

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
    internal struct ddTableDealPBN
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string cards;

        public ddTableDealPBN(string hands)
        {
            cards = hands;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class ddTableDealsPBN
    {
        public int noOfTables;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains)]
        public ddTableDealPBN[] deals;

        public ddTableDealsPBN(List<string> hands)
        {
            noOfTables = hands.Count;
            deals = new ddTableDealPBN[ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains];
            for (int hand = 0; hand < hands.Count; hand++) deals[hand] = new ddTableDealPBN(hands[hand]);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ddTableDeal
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public uint[,] cards;

        public ddTableDeal(Deal deal)
        {
            cards = new uint[4, 4];
            for (int seat = 0; seat < 4; seat++)
            {
                for (int suit = 0; suit < 4; suit++)
                {
                    for (int rank = 2; rank <= 14; rank++)
                    {
                        if (deal[seat, suit, rank])
                        {
                            cards[(int)(seat), (int)suit] |= (uint)(2 << ((int)rank) - 1);
                        }
                    }
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class ddTableDeals
    {
        public int noOfTables;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains)]
        public ddTableDeal[] tableDeals;

        public ddTableDeals(List<Deal> deals)
        {
            noOfTables = deals.Count;
            tableDeals = new ddTableDeal[ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains];
            for (int hand = 0; hand < deals.Count; hand++) tableDeals[hand] = new ddTableDeal(deals[hand]);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ddTablesResult
    {
        public int noOfBoards;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains)]
        public ddTableResults[] results;

        public ddTablesResult(int deals)
        {
            noOfBoards = deals;
            results = new ddTableResults[ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains];
            for (int deal = 0; deal < deals; deal++) results[deal] = new ddTableResults();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct parResults
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public parScore[] parScores;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public parContract[] parContracts;

        public parResults()
        {
            parScores = new parScore[2];
            parContracts = new parContract[2];
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct parScore
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string score;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct parContract
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string score;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct allParResults
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public parResults[] results;

        public allParResults()
        {
            results = new parResults[20];
            for (int i = 0; i < 20; i++) results[i] = new parResults();
        }
    }

    internal struct dealPBN
    {
        public int trump;

        public int first;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public int[] currentTrickSuit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public int[] currentTrickRank;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string remainCards;

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

    [StructLayout(LayoutKind.Sequential)]
    internal struct DDSInfo
    {
        public int major, minor, patch;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string versionString;

        // Currently 0 = unknown, 1 = Windows, 2 = Cygwin, 3 = Linux, 4 = Apple
        public int system;

        // We know 32 and 64-bit systems.
        public int numBits;

        // Currently 0 = unknown, 1 = Microsoft Visual C++, 2 = mingw,
        // 3 = GNU g++, 4 = clang
        public int compiler;

        // Currently 0 = none, 1 = DllMain, 2 = Unix-style
        public int constructor;

        public int numCores;

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
        public int threading;

        // The actual number of threads configured
        public int noOfThreads;

        // This will break if there are > 128 threads...
        // The string is of the form LLLSSS meaning 3 large TT memories
        // and 3 small ones.
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string threadSizes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string systemString;
    }

    /*
struct DDSInfo
{
  // Version 2.8.0 has 2, 8, 0 and a string of 2.8.0
  int major, minor, patch; 
  char versionString[10];
};
     * */
    [StructLayout(LayoutKind.Sequential)]
    internal struct FutureTricks
    {
        /// <summary>
        /// /* Number of searched nodes */
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int nodes;

        /// <summary>
        /// /*  No of alternative cards  */
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int cards;

        /// <summary>
        /// /* 0=Spades, 1=Hearts, 2=Diamonds, 3=Clubs */
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public int[] suit;

        /// <summary>
        /// /* 2-14 for 2 through Ace */ 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public int[] rank;

        /// <summary>
        /// /* Bit string of ranks for equivalent lower rank cards. The decimal value range between 4 (=2) and 8192 (King=rank 13). 
        ///  When there are several ”equals”, the value is the sum of each ”equal”. */
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public int[] equals;

        /// <summary>
        /// /* -1 indicates that target was not reached, otherwise target or max numbe of tricks */ 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public int[] score;
    }

    public enum Suit { Spades = 0, Hearts = 1, Diamonds = 2, Clubs = 3, NT = 4}

    public enum Rank { Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10, Jack = 11, Queen = 12, King = 13, Ace = 14 }

    public enum Hand { North = 0, East = 1, South = 2, West = 3 }

    public enum Vulnerable { None = 0, Both = 1, NSonly = 2, EWonly = 3 }

    public struct Card
    {
        public Suit Suit { get; set; }
        public Rank Rank { get; set; }
        public override string ToString() => $"{Suit.ToString()[0]}{(Rank < Rank.Ten ? ((int)Rank).ToString() : Rank.ToString()[0])}";
    }
}
