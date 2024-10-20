using System.Diagnostics;

namespace DDS
{
    public static class ddsWrapper
    {
        public static List<CardPotential> SolveBoard(GameState state)
        {
            // Parameter ”target” is the number of tricks to be won by the side to play, 
            // -1 means that the program shall find the maximum number.
            // For equivalent  cards only the highest is returned.
            // target=1-13, solutions=1:  Returns only one of the cards. 
            // Its returned score is the same as target whentarget or higher tricks can be won. 
            // Otherwise, score –1 is returned if target cannot be reached, or score 0 if no tricks can be won. 
            // target=-1, solutions=1:  Returns only one of the optimum cards and its score.
            var deal = new dealPBN(state.Trump, state.TrickLeader, state.TrickCards.ToArray(), state.RemainingCards.ToPBN());
            int target = -1;
            int solutions = 3;
            int mode = 0;
            int threadIndex = 0;
            var futureTricks = new FutureTricks();
            var hresult = ddsImports.SolveBoardPBN(deal, target, solutions, mode, ref futureTricks, threadIndex);
            Inspect(hresult);

            var result = new List<CardPotential>();
            for (int i = 0; i < futureTricks.cards; i++)
            {
                result.Add(new CardPotential { Tricks = futureTricks.score[i], Card = new Card { Suit = (Suit)futureTricks.suit[i], Rank = (Rank)futureTricks.rank[i] } });
            }
            return result;
        }

        public static TableResults PossibleTricks(string pbn)
        {
            var deal = new ddTableDealPBN { cards = pbn };
            var results = new ddTableResults();
            var hresult = ddsImports.CalcDDtablePBN(deal, ref results);
            Inspect(hresult);

            TableResults result;
            DdsEnum.ForEachHand(hand =>
            {
                DdsEnum.ForEachTrump(suit =>
                {
                    result[hand, suit] = results[hand, suit];
                });
            });
            return result;
        }

        public static List<TableResults> PossibleTricks(List<Deal> deals, List<Suit> trumps)
        {
            if (trumps == null || trumps.Count == 0) trumps = [ Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades, Suit.NT ];
            var tableDeals = new ddTableDealsPBN(deals.Select(d => d.ToPBN()).ToList());
            var results = new ddTablesResult(deals.Count);
            var parResults = new allParResults();

            var hresult = ddsImports.CalcAllTablesPBN(tableDeals, -1, Convert(trumps), ref results, ref parResults);
            Inspect(hresult);

            var result = new List<TableResults>();
            for (int deal = 0; deal < deals.Count; deal++)
            {
                TableResults tableResult;
                DdsEnum.ForEachHand(hand =>
                {
                    DdsEnum.ForEachTrump(suit =>
                    {
                        tableResult[hand, suit] = (results.results[deal])[hand, suit];
                    });
                });
                result.Add(tableResult);
            }

            return result;
        }

        public static List<TableResults> PossibleTricks2(List<Deal> deals, List<Suit> trumps)
        {
            var tableDeals = new ddTableDeals(deals);
            var results = new ddTablesResult(deals.Count);
            var parResults = new allParResults();

            var hresult = ddsImports.CalcAllTables(tableDeals, -1, Convert(trumps), ref results, ref parResults);
            Inspect(hresult);

            var result = new List<TableResults>();
            for (int deal = 0; deal < deals.Count; deal++)
            {
                TableResults tableResult;
                DdsEnum.ForEachHand(hand =>
                {
                    DdsEnum.ForEachTrump(suit =>
                    {
                        tableResult[hand, suit] = (results.results[deal])[hand, suit];
                    });
                });
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
            switch (returnCode)
            {
                case 1: return;     // no fault
                case -1: throw new Exception("dds unknown fault");
                case -2: throw new Exception("dds SolveBoard: 0 cards");
                case -10: throw new Exception("dds SolveBoard: too many cards");
                case -12: throw new Exception("dds SolveBoard: either currentTrickSuit or currentTrickRank have wrong data");
                default: throw new Exception("dds undocumented fault");
            }
        }

        #region converters

        #endregion
    }
}
