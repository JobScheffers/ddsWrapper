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
    public struct ddTableResults
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        private int[] resTable;

        public ddTableResults()
        {
            resTable = new int[20];
        }

        public int this[Hand hand, Suit suit]
        {
            get { return resTable[4 * (int)suit + (int)hand]; }
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
            foreach (Hand seat in Enum.GetValues(typeof(Hand)))
            {
                foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                {
                    foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                    {
                        if (deal[seat, suit, rank])
                        {
                            cards[(int)(seat), 3 - (int)suit] |= (uint)(2 << ((int)rank));
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

        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        //public char[] remainCards;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string remainCards;

        public dealPBN(Suit _trump, Hand trickLeader, Card[] currentTrick, string remainingCards)
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
    public struct FutureTricks
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
    }
}
