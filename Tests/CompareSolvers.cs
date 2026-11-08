using Bridge;
using BridgeSolver;
using DDS;
using DDS.Interop;
using System.Diagnostics;

namespace Tests
{
    [TestClass]
    public class CompareSolvers
    {
        [TestMethod]
        [DataRow("N:AKQJT98765432... .AKQJT98765432.. ..AKQJT98765432. ...AKQJT98765432")]
        [DataRow("N:AKQT3.J6.KJ42.95 652.AK42.AQ87.T4 J74.QT95.T.AK863 98.873.9653.QJ72")]
        [DataRow("N:AQJ7.832.853.T93 .974.QJ72.AQ8652 T952.AT.KT96.J74 K8643.KQJ65.A4.K")]
        public void CompareSolvers_KnownDeal_ReturnsEqualNumbers(string deal)
        {
            var tricksSolver = BridgeSolverWrapper.DdTableTricks(deal);
            var tricksDds = ddsWrapper.PossibleTricks(deal);

            //Trace.WriteLine(deal);
            //Trace.WriteLine("       C  D  H  S  NT");
            //DdsEnum.ForEachHand(seat =>
            //{
            //    Trace.Write($"{seat,-5}");
            //    DdsEnum.ForEachTrump(suit =>
            //    {
            //        Trace.Write($" {tricksSolver[seat, suit]:00} {tricksDds[seat, suit]:00}");
            //    });
            //    Trace.WriteLine($"");
            //});

            DdsEnum.ForEachHand(seat =>
            {
                DdsEnum.ForEachTrump(suit =>
                {
                    Assert.AreEqual(tricksSolver[seat, suit], tricksDds[seat, suit]);
                });
            });

        }

        [TestMethod]
        public void PossibleTricks_410Deals_5Trumps()
        {
            RandomGenerator.Instance.Repeatable(1);
            var baseDeal = new Deal();
            var deals = new List<Deal>();
            const int numDeals = 410;
            for (int i = 0; i < numDeals; i++)
            {
                deals.Add(baseDeal.CompletedFromSeed(RandomGenerator.Instance.NextDealBigInteger()));
            }

            var trumps = new List<Suits> { Suits.Clubs, Suits.Diamonds, Suits.Hearts, Suits.Spades, Suits.NoTrump };
            TableResults needed = new TableResults();
            needed[Seats.North, Suits.Clubs] = 1;
            //var trumps = new List<Suits> { Suits.Clubs, Suits.Diamonds, Suits.Hearts };
            var timeAllowed = 5; // seconds

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result1 = ddsWrapper.PossibleTricks(deals, trumps, maxDuration: timeAllowed);
            sw.Stop();
            Trace.WriteLine($"DDS 2.8: {result1.Count} deals in {sw.Elapsed.TotalSeconds:F1}s; {sw.Elapsed.TotalSeconds / result1.Count:F3}s per deal");

            sw.Restart();
            var result2 = BridgeSolverWrapper.PossibleTricks(deals, needed, maxDuration: timeAllowed);
            sw.Stop();
            Trace.WriteLine($"Solver : {result2.Count} deals in {sw.Elapsed.TotalSeconds:F1}s; {sw.Elapsed.TotalSeconds / result2.Count:F3}s per deal");

            // Compare results
            if (result1.Count == result2.Count)
            {
                for (int i = 0; i < result1.Count; i++)
                {
                    DdsEnum.ForEachHand(seat =>
                    {
                        DdsEnum.ForEachTrump(suit =>
                        {
                            if (result2[i][seat, suit] <= 13)
                                Assert.AreEqual(result1[i][seat, suit], result2[i][seat, suit]);
                        });
                    });
                }
            }
        }

        [TestMethod]
        public void Compare_BestCards()
        {
            RandomGenerator.Instance.Repeatable(1);
            var baseDeal = new Deal("N:AQJ7.832.853.T93");
            var deals = new List<GameState>();
            const int numDeals = 500;
            for (int i = 0; i < numDeals; i++)
            {
                var deal = baseDeal.CompletedFromSeed(RandomGenerator.Instance.NextDealBigInteger());
                var state = new GameState(in deal, 13, Suits.Hearts, Seats.West, Seats.North, Card.Null.Index, Card.Null.Index, Card.Null.Index);
                deals.Add(state);
            }

            var sw = Stopwatch.StartNew();
            //var result1 = ddsWrapper.PossibleTricks(deals, trumps, maxDuration: timeAllowed);
            //sw.Stop();
            //Trace.WriteLine($"DDS 2.8: {result1.Count} deals in {sw.Elapsed.TotalSeconds:F1}s; {sw.Elapsed.TotalSeconds / result1.Count:F3}s per deal");
            Parallel.For(0, numDeals, deal =>
            {
                var result = ddsWrapper.BestCards(deals[deal]);
            });
            sw.Stop();
            Trace.WriteLine($"DDS 2.8 : {numDeals} deals in {sw.Elapsed.TotalSeconds:F1}s; {sw.Elapsed.TotalSeconds / numDeals:F3}s per deal");

            sw.Restart();
            Parallel.For(0, numDeals, deal =>
            {
                var result = BridgeSolverWrapper.BestCards(deals[deal], new List<Card>() { Card.Get(Suits.Spades, Ranks.Ace), Card.Get(Suits.Spades, Ranks.Queen), Card.Get(Suits.Hearts, Ranks.Eight), Card.Get(Suits.Hearts, Ranks.Two) });
            });
            sw.Stop();
            Trace.WriteLine($"Solver  : {numDeals} deals in {sw.Elapsed.TotalSeconds:F1}s; {sw.Elapsed.TotalSeconds / numDeals:F3}s per deal");

            // Compare results
            //if (result1.Count == result2.Count)
            //{
            //    for (int i = 0; i < result1.Count; i++)
            //    {
            //        DdsEnum.ForEachHand(seat =>
            //        {
            //            DdsEnum.ForEachTrump(suit =>
            //            {
            //                if (result2[i][seat, suit] <= 13)
            //                    Assert.AreEqual(result1[i][seat, suit], result2[i][seat, suit]);
            //            });
            //        });
            //    }
            //}
        }
    }
}
