using System.Diagnostics;

namespace DDS
{
    public static class ddsWrapper
    {
        private static readonly object locker = new object();
        private static readonly bool[] threadOccupied = new bool[16];

        private static List<CardPotential> SolveBoard(ref readonly GameState state, int target, int solutions, int mode)
        {
            // Parameter ”target” is the number of tricks to be won by the side to play, 
            // -1 means that the program shall find the maximum number.
            // For equivalent  cards only the highest is returned.
            // target=1-13, solutions=1:  Returns only one of the cards. 
            // Its returned score is the same as target when target or higher tricks can be won. 
            // Otherwise, score –1 is returned if target cannot be reached, or score 0 if no tricks can be won. 
            // target=-1, solutions=1:  Returns only one of the optimum cards and its score.
            var result = new List<CardPotential>();
            var trickCards = state.TrickCards.ToArray();
            var deal = new deal(state.Trump, state.TrickLeader, ref trickCards, state.RemainingCards);
            var futureTricks = new FutureTricks();

            var hresult = 0;
            var threadIndex = GetThreadIndex();
            try
            {
                hresult = ddsImports.SolveBoard(deal, target, solutions, mode, ref futureTricks, threadIndex);
            }
            finally
            {
                ReleaseThreadIndex(threadIndex);
            }

            Inspect(hresult);

            for (int i = 0; i < futureTricks.cards; i++)
            {
                result.Add(new CardPotential(new Card((Suit)futureTricks.suit[i], (Rank)futureTricks.rank[i]), futureTricks.score[i], futureTricks.equals[i] == 0));
                var firstEqual = true;
                for (Rank rank = Rank.Two; rank <= Rank.Ace; rank++)
                {
                    if ((futureTricks.equals[i] & ((uint)(2 << ((int)rank) - 1))) > 0)
                    {
                        result.Add(new CardPotential(new Card((Suit)futureTricks.suit[i], rank), futureTricks.score[i], firstEqual));
                        firstEqual = false;
                    }
                };
            }
            return result;

            int GetThreadIndex()
            {
                while (true)
                {
                    lock (locker)
                    {
                        for (int i = 0; i < ddsImports.MaxThreads; i++)
                        {
                            if (!threadOccupied[i])
                            {
                                threadOccupied[i] = true;
                                return i;
                            }
                        }
                    }

                    Thread.Sleep(50);
                }
            }

            void ReleaseThreadIndex(int threadIndex)
            {
                lock (locker)
                {
                    threadOccupied[threadIndex] = false;
                }
            }
        }

        public static List<CardPotential> BestCards(ref readonly GameState state)
        {
            return SolveBoard(in state, -1, 2, 1);
        }

        public static List<CardPotential> BestCard(ref readonly GameState state)
        {
            return SolveBoard(in state, -1, 1, 1);
        }

        public static List<CardPotential> AllCards(ref readonly GameState state)
        {
            return SolveBoard(in state, 0, 3, 1);
        }

        public static TableResults PossibleTricks(string pbn)
        {
            var deal = new ddTableDealPBN(pbn);
            var results = new ddTableResults();
            var hresult = ddsImports.CalcDDtablePBN(deal, ref results);
            Inspect(hresult);

            TableResults result;
            for (Hand hand = Hand.North; hand <= Hand.West; hand++)
            {
                for (Suit suit = Suit.Spades; suit <= Suit.NT; suit++)
                {
                    result[hand, suit] = results[hand, suit];
                };
            };
            return result;
        }

        public static List<TableResults> PossibleTricks(List<Deal> deals, List<Suit> trumps)
        {
            if (trumps == null || trumps.Count == 0) trumps = [ Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades, Suit.NT ];
            var tableDeals = new ddTableDeals(deals);
            var results = new ddTablesResult(deals.Count);
            var parResults = new allParResults();

            var hresult = ddsImports.CalcAllTables(tableDeals, -1, Convert(trumps), ref results, ref parResults);
            Inspect(hresult);

            var result = new List<TableResults>();
            for (int deal = 0; deal < deals.Count; deal++)
            {
                TableResults tableResult;
                for (Hand hand = Hand.North; hand <= Hand.West; hand++)
                {
                    for (Suit suit = Suit.Spades; suit <= Suit.NT; suit++)
                    {
                        tableResult[hand, suit] = (results.results[deal])[hand, suit];
                    };
                };
                result.Add(tableResult);
            }

            return result;
        }

        private static int[] Convert(List<Suit> trumps)
        {
            var result = new int[5] { 1, 1, 1, 1, 1 };
            foreach (Suit suit in trumps)
            {
                result[(int)suit] = 0;
            }
            return result;
        }

        private static void Inspect(int returnCode)
        {
            if (returnCode == 1) return;
            //throw new Exception(Error(returnCode));

            switch (returnCode)
            {
                case 1: return;     // no fault
                case -1: throw new Exception("dds unknown fault");
                case -2: throw new Exception("dds SolveBoard: 0 cards");
                case -4: throw new Exception("dds SolveBoard: duplicate cards");
                case -10: throw new Exception("dds SolveBoard: too many cards");
                case -12: throw new Exception("dds SolveBoard: either currentTrickSuit or currentTrickRank have wrong data");
                case -14: throw new Exception("dds SolveBoard: wrong number of remaining cards for a hand");
                case -15: throw new Exception("dds SolveBoard: thread number is less than 0 or higher than the maximum permitted");
                case -201: throw new Exception("dds CalcAllTables: the denomination filter vector has no entries");
                default: throw new Exception($"dds undocumented fault {returnCode}");
            }
        }

        public static string Error(int returnCode)
        {
            if (returnCode == 1) return "";
            var error = new Char[80];
            ddsImports.ErrorMessage(returnCode, error);
            return new string(error);
        }

        #region converters

        #endregion
    }
}
