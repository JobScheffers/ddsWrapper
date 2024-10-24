using DDS;
using System.Diagnostics;

namespace Tests
{
    [TestClass]
    public class DdsTests
    {

        [TestMethod]
        public void SolveBoard2()
        {
            //         s JT984
            //         h T7
            //         d AQ83
            //         c 4
            //s A6            s Q7532
            //h K964          h 82
            //d T65           d 97
            //c Q96           c 832
            //         s K
            //         h AQJ53
            //         d KJ42
            //         c AK
            string cards = "N:JT984.T7.AQ83.4 Q7532.82.97.832 K.AQJ53.KJ42.AK A6.K964.T65.Q96";

            var result = ddsWrapper.SolveBoard(new GameState { Trump = Suit.Hearts, TrickLeader = Hand.South, RemainingCards = new Deal(cards), TrickCards = [] });
            Assert.AreEqual(11, result.Count);
        }

        [TestMethod]
        public void SolveBoard1()
        {
            //         s T9
            //         h 2
            //         d 732
            //         c T
            //s               s 
            //h A874          h JT5
            //d K9            d T4
            //c               c J4
            //         s 54
            //         h 
            //         d 
            //         c A9862
            string cards = "N:T9.2.732.T .JT5.T4.J4 54...A9862 .A874.K9.";

            var result = ddsWrapper.SolveBoard(new GameState { Trump = Suit.Spades, TrickLeader = Hand.West, RemainingCards = new Deal(cards), TrickCards = [new Card { Suit = Suit.Hearts, Rank = Rank.King }] });
            Assert.AreEqual(7, result[0].Tricks);
        }

        [TestMethod]
        public void SolveBoard()
        {
            string gamestate3 = "N:K95.QJT3.AKJ.AQJ JT42.87..K98765 AQ86.K652.86432. 73.A94.QT97.T432";

            var result =
                Profiler.Time(() =>
                {
                    return ddsWrapper.SolveBoard(new GameState { Trump = Suit.Hearts, TrickLeader = Hand.East, RemainingCards = new Deal(gamestate3), TrickCards = [new Card { Suit = Suit.Diamonds, Rank = Rank.Five }] });
                }, out var elapsedTime, 100);
            Trace.WriteLine($"1 call took {elapsedTime.TotalMilliseconds/100:F2} ms");
            Assert.AreEqual(11, result[0].Tricks);
        }

        [TestMethod]
        public void CalcAllTables()
        {
            var deal1 = new Deal("N:954.QJT3.AJT.QJ6 KJT2.87.5.AK9875 AQ86.K9654.8643. 73.A2.KQ972.T432");
            var deal2 = new Deal("N:954.QJT3.AKJ.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.QT97.T432");
            var deal3 = new Deal("N:K95.QJT3.AKJ.AQJ JT42.87.5.K98765 AQ86.K652.86432. 73.A94.QT97.T432");

            var result =
                Profiler.Time(() =>
                {
                    return ddsWrapper.PossibleTricks(new List<Deal> { deal1, deal2, deal3 }, []);
                }, out var elapsedTime, 10);

            Trace.WriteLine($"took {elapsedTime.TotalMilliseconds:F0} ms");
            foreach (var deal in result)
            {
                Trace.WriteLine("       C  D  H  S  NT");
                DdsEnum.ForEachHand(seat =>
                {
                    Trace.Write($"{seat.ToString().PadRight(5)}");
                    DdsEnum.ForEachTrump(suit =>
                    {
                        Trace.Write($" {deal[seat, suit]:00}");
                    });
                    Trace.WriteLine($"");
                });
            }

            Assert.AreEqual(8, result[0][Hand.North, Suit.Spades]);
            Assert.AreEqual(11, result[2][Hand.North, Suit.Hearts]);
        }

        [TestMethod]
        public void CalcDDtablePBN()
        {
            //

            string deal = "N:AQJ7.832.853.T93 .974.QJ72.AQ8652 T952.AT.KT96.J74 K8643.KQJ65.A4.K";

            var result =
                Profiler.Time(() =>
                {
                    return ddsWrapper.PossibleTricks(deal);
                }, out var elapsedTime);

            Trace.WriteLine($"took {elapsedTime.TotalMilliseconds:F0} ms");
            Trace.WriteLine(deal);
            Trace.WriteLine("       C  D  H  S  NT");
            DdsEnum.ForEachHand(seat =>
            {
                Trace.Write($"{seat.ToString().PadRight(5)}");
                DdsEnum.ForEachTrump(suit =>
                {
                    Trace.Write($" {result[seat, suit]:00}");
                });
                Trace.WriteLine($"");
            });

            //        C  D  H  S NT
            // North 01 05 02 06 04
            // East  12 08 11 07 08
            // South 01 05 02 06 04
            // West  12 08 11 07 09
            Assert.AreEqual(4, result[Hand.North, Suit.NT]);
            Assert.AreEqual(6, result[Hand.North, Suit.Spades]);
            Assert.AreEqual(2, result[Hand.North, Suit.Hearts]);
            Assert.AreEqual(5, result[Hand.North, Suit.Diamonds]);
            Assert.AreEqual(1, result[Hand.North, Suit.Clubs]);
            Assert.AreEqual(8, result[Hand.East, Suit.NT]);
            Assert.AreEqual(7, result[Hand.East, Suit.Spades]);
            Assert.AreEqual(11, result[Hand.East, Suit.Hearts]);
            Assert.AreEqual(8, result[Hand.East, Suit.Diamonds]);
            Assert.AreEqual(12, result[Hand.East, Suit.Clubs]);
            Assert.AreEqual(4, result[Hand.South, Suit.NT]);
            Assert.AreEqual(6, result[Hand.South, Suit.Spades]);
            Assert.AreEqual(2, result[Hand.South, Suit.Hearts]);
            Assert.AreEqual(5, result[Hand.South, Suit.Diamonds]);
            Assert.AreEqual(1, result[Hand.South, Suit.Clubs]);
            Assert.AreEqual(9, result[Hand.West, Suit.NT]);
            Assert.AreEqual(7, result[Hand.West, Suit.Spades]);
            Assert.AreEqual(11, result[Hand.West, Suit.Hearts]);
            Assert.AreEqual(8, result[Hand.West, Suit.Diamonds]);
            Assert.AreEqual(12, result[Hand.West, Suit.Clubs]);
        }
    }
}