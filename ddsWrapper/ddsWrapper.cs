using System.Diagnostics;

namespace DDS
{
    public static class ddsWrapper
    {
        public static FutureTricks SolveBoard(Suit trump, Hand trickLeader, string remainingCardsPbn)
        {
            // Parameter ”target” is the number of tricks to be won by the side to play, 
            // -1 means that the program shall find the maximum number.
            // For equivalent  cards only the highest is returned.
            // target=1-13, solutions=1:  Returns only one of the cards. 
            // Its returned score is the same as target whentarget or higher tricks can be won. 
            // Otherwise, score –1 is returned if target cannot be reached, or score 0 if no tricks can be won. 
            // target=-1, solutions=1:  Returns only one of the optimum cards and its score.
            var deal = new dealPBN(trump, trickLeader, Array.Empty<Card>(), remainingCardsPbn);
            int target = -1;
            int solutions = 3;
            int mode = 0;
            int threadIndex = 0;
            var futureTricks = new FutureTricks();
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            var hresult = ddsImports.SolveBoardPBN(deal, target, solutions, mode, ref futureTricks, threadIndex);
#if DEBUG
            stopwatch.Stop();
            Trace.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
#endif
            Inspect(hresult);


            return futureTricks;
        }

        public static ddTableResults PossibleTricks(string pbn)
        {
            var deal = new ddTableDealPBN { cards = pbn };
            var results = new ddTableResults();
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            var hresult = ddsImports.CalcDDtablePBN(deal, ref results);
#if DEBUG
            stopwatch.Stop();
            Trace.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
#endif
            Inspect(hresult);

            return results;
        }

        public static List<ddTableResults> PossibleTricks(List<string> pbns)
        {
            return PossibleTricks(pbns, new SuitCollection<bool>([true, true, true, true, true]));
        }

        public static List<ddTableResults> PossibleTricks(List<string> pbns, SuitCollection<bool> trumps)
        {
            var deals = new ddTableDealsPBN(pbns);
            var results = new ddTablesResult(pbns.Count);
            var parResults = new allParResults();

#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            var hresult = ddsImports.CalcAllTablesPBN(deals, -1, Convert(trumps), ref results, ref parResults);
#if DEBUG
            stopwatch.Stop();
            Trace.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
#endif
            Inspect(hresult);

            var result = new List<ddTableResults>();
            for (int deal = 0; deal < pbns.Count; deal++)
            {
                result.Add(results.results[deal]);
            }

            return result;

            int[] Convert(SuitCollection<bool> trump)
            {
                var result = new int[5];
                DdsEnum.ForEachTrump(suit =>
                {
                    result[(int)suit] = trump[suit] ? 0 : 1;
                });
                return result;
            }
        }

        public static List<ddTableResults> PossibleTricks2(List<Deal> deals, SuitCollection<bool> trumps)
        {
            var tableDeals = new ddTableDeals(deals);
            var results = new ddTablesResult(deals.Count);
            var parResults = new allParResults();

#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            var hresult = ddsImports.CalcAllTables(tableDeals, -1, Convert(trumps), ref results, ref parResults);
#if DEBUG
            stopwatch.Stop();
            Trace.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
#endif
            Inspect(hresult);

            var result = new List<ddTableResults>();
            for (int deal = 0; deal < deals.Count; deal++)
            {
                result.Add(results.results[deal]);
            }

            return result;

            int[] Convert(SuitCollection<bool> trump)
            {
                var result = new int[5];
                DdsEnum.ForEachTrump(suit =>
                {
                    result[(int)suit] = trump[suit] ? 0 : 1;
                });
                return result;
            }
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
