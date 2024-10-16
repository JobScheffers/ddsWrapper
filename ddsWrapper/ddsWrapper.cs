using Bridge;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DDS
{
    public static class ddsWrapper
    {
        public static SeatsSuitsArrayOfByte PossibleTricks(string pbn)
        {
            ddTableDealPBN deal = new ddTableDealPBN { cards = pbn };
            ddTableResults results = new ddTableResults();
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            var hresult = CalcDDtablePBN(deal, ref results);
#if DEBUG
            stopwatch.Stop();
            Trace.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
#endif
            Inspect(hresult);

            var result = new SeatsSuitsArrayOfByte();
            SeatsExtensions.ForEachSeat(seat =>
            {
                SuitHelper.ForEachTrump(suit =>
                {
                    result[seat, suit] = (byte)results.resTable[ddsSuit(suit)].results[ddsSeat(seat)];
                });
            });

            return result;
        }

        public static List<SeatsTrumpsArrayOfByte> PossibleTricks(List<string> pbns)
        {
            return PossibleTricks(pbns, new SuitCollection<bool>([true, true, true, true, true]));
        }

        public static List<SeatsTrumpsArrayOfByte> PossibleTricks(List<string> pbns, SuitCollection<bool> trumps)
        {
            var deals = new ddTableDealsPBN(pbns);
            var results = new ddTablesResult(pbns.Count);
            var parResults = new allParResults();

#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            var hresult = CalcAllTablesPBN(deals, -1, Convert(trumps), ref results, ref parResults);
#if DEBUG
            stopwatch.Stop();
            Trace.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
#endif
            Inspect(hresult);

            var result = new List<SeatsTrumpsArrayOfByte>();
            for (int deal = 0; deal < pbns.Count; deal++)
            {
                var dealResult = new SeatsTrumpsArrayOfByte();
                SeatsExtensions.ForEachSeat(seat =>
                {
                    SuitHelper.ForEachTrump(suit =>
                    {
                        dealResult[seat, suit] = (byte)results.results[deal].resTable[ddsSuit(suit)].results[ddsSeat(seat)];
                    });
                });
                result.Add(dealResult);
            }

            return result;

            int[] Convert(SuitCollection<bool> trump)
            {
                var result = new int[5];
                SuitHelper.ForEachTrump(suit =>
                {
                    result[ddsSuit(suit)] = trump[suit] ? 0 : 1;
                });
                return result;
            }
        }

        public static List<SeatsTrumpsArrayOfByte> PossibleTricks2(List<Deal> deals, SuitCollection<bool> trumps)
        {
            var tableDeals = new ddTableDeals(deals);
            var results = new ddTablesResult(deals.Count);
            var parResults = new allParResults();

#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            var hresult = CalcAllTables(tableDeals, -1, Convert(trumps), ref results, ref parResults);
#if DEBUG
            stopwatch.Stop();
            Trace.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
#endif
            Inspect(hresult);

            var result = new List<SeatsTrumpsArrayOfByte>();
            for (int deal = 0; deal < deals.Count; deal++)
            {
                var dealResult = new SeatsTrumpsArrayOfByte();
                SeatsExtensions.ForEachSeat(seat =>
                {
                    SuitHelper.ForEachTrump(suit =>
                    {
                        dealResult[seat, suit] = (byte)results.results[deal].resTable[ddsSuit(suit)].results[ddsSeat(seat)];
                    });
                });
                result.Add(dealResult);
            }

            return result;

            int[] Convert(SuitCollection<bool> trump)
            {
                var result = new int[5];
                SuitHelper.ForEachTrump(suit =>
                {
                    result[ddsSuit(suit)] = trump[suit] ? 0 : 1;
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
        const int ddsMaxNumberOfBoards = 200;
        const int ddsStrains = 5;

        #region Imports

        [DllImport("dds2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CalcDDtablePBN(ddTableDealPBN tableDealPBN, ref ddTableResults tablep);

        [DllImport("dds2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CalcAllTablesPBN(ddTableDealsPBN deals, int mode, int[] trumpFilter, ref ddTablesResult tableResults, ref allParResults parResults);

        [DllImport("dds2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CalcAllTables(ddTableDeals deals, int mode, int[] trumpFilter, ref ddTablesResult tableResults, ref allParResults parResults);

        [DllImport("dds2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int DealerPar(ref ddTableResults tablep, ref parResultsDealer presp, int dealer, int vulnerable);

        #endregion

        #region Parameter structs

        [StructLayout(LayoutKind.Sequential)]
        private struct parResultsDealer
        {
            public int number;
            public int score;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
            public string contracts;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ddTableResults
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public handResults[] resTable;

            public ddTableResults()
            {
                resTable = new handResults[5];
                for (int hand = 0; hand <= 4; hand++) resTable[hand] = new handResults();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct handResults
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public int[] results;

            public handResults()
            {
                results = new int[4];
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ddTableDealPBN
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string cards;

            public ddTableDealPBN(string hands)
            { cards = hands; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private class ddTableDealsPBN
        {
            public int noOfTables;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsMaxNumberOfBoards * ddsStrains)]
            public ddTableDealPBN[] deals;

            public ddTableDealsPBN(List<string> hands)
            {
                noOfTables = hands.Count;
                deals = new ddTableDealPBN[ddsMaxNumberOfBoards * ddsStrains];
                for (int hand = 0; hand < hands.Count; hand++) deals[hand] = new ddTableDealPBN(hands[hand]);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ddTableDeal
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[,] cards;

            public ddTableDeal(Deal deal)
            {
                cards = new uint[4,4];
                for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                {
                    for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                    {
                        for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
                        {
                            if (deal[seat, suit, rank])
                            {
                                cards[(int)(seat), 3 - (int)suit] |= (uint)(2 << ((int)rank + 2));
                            }
                        }
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private class ddTableDeals
        {
            public int noOfTables;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsMaxNumberOfBoards * ddsStrains)]
            public ddTableDeal[] tableDeals;

            public ddTableDeals(List<Deal> deals)
            {
                noOfTables = deals.Count;
                tableDeals = new ddTableDeal[ddsMaxNumberOfBoards * ddsStrains];
                for (int hand = 0; hand < deals.Count; hand++) tableDeals[hand] = new ddTableDeal(deals[hand]);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ddTablesResult
        {
            public int noOfBoards;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsMaxNumberOfBoards * ddsStrains)]
            public ddTableResults[] results;

            public ddTablesResult(int deals)
            {
                noOfBoards = deals;
                results = new ddTableResults[ddsMaxNumberOfBoards * ddsStrains];
                for (int deal = 0; deal < deals; deal++) results[deal] = new ddTableResults();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct parResults
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
        private struct parScore
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string score;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct parContract
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string score;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct allParResults
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public parResults[] results;

            public allParResults()
            {
                results = new parResults[20];
                for (int i = 0; i < 20; i++) results[i] = new parResults();
            }
        }

        #endregion

        #region converters

        private static int ddsSeat(Seats seat)
        {
            switch (seat)
            {
                case Seats.North: return 0;
                case Seats.East: return 1;
                case Seats.South: return 2;
                default: return 3;
            }
        }

        private static int ddsSuit(Suits suit)
        {
            switch (suit)
            {
                case Suits.Clubs: return 3;
                case Suits.Diamonds: return 2;
                case Suits.Hearts: return 1;
                case Suits.Spades: return 0;
                default: return 4;
            }
        }

        #endregion

        static ddsWrapper()
        {
            ResourceExtractor.ExtractResourceToFile("ddsWrapper.dds.dll", "dds2.dll");
        }
    }

    public static class ResourceExtractor
    {
        public static void ExtractResourceToFile(string resourceName, string filename)
        {
            if (!File.Exists(filename))
                using (Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!)
                using (FileStream fs = new FileStream(filename, FileMode.Create))
                {
                    byte[] b = new byte[s.Length];
                    s.Read(b, 0, b.Length);
                    fs.Write(b, 0, b.Length);
                }
        }
    }
}
