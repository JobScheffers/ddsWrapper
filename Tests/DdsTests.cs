using Bridge;
using DDS;
using System.Diagnostics;

namespace Tests
{
    [TestClass]
    public class DdsTests
    {

        [TestMethod]
        public void SolveBoard_From_Multiple_Threads()
        {
            Parallel.For(1, 1000, i =>
            {
                SolveBoard3();
            });
        }

        [TestMethod]
        public void SolveBoard3()
        {
            //         s 9
            //         h
            //         d 85432
            //         c QJ9
            //s AKQT63        s 754
            //h 5             h JT73
            //d               d KT
            //c 8             c
            //         s J82
            //         h KQ6
            //         d QJ
            //         c 6
            string cards = "N:9..85432.QJ9 754.JT73.KT. J82.KQ6.QJ.6 AKQT63.5..8";
            var deal = new DDS.Deal(ref cards);
            var state = new GameState(in deal, Suits.Spades, Seats.West, CardDeck.Instance[Suits.Clubs, Ranks.Seven], Bridge.Card.Null, Bridge.Card.Null );
            var result = ddsWrapper.BestCards(in state);
            Assert.AreEqual(3, result.Count);
            Assert.IsFalse(result[0].IsPrimary);
            Assert.IsTrue(result[1].IsPrimary);
        }

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
            var deal = new DDS.Deal(ref cards);
            var state = new GameState(in deal, Suits.Hearts, Seats.South);
            var result = ddsWrapper.BestCards(ref state);
            Assert.AreEqual(12, result.Count);
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
            ddsWrapper.ForgetPreviousBoard();
            string cards = "N:T9.2.732.T .JT5.T4.J4 54...A9862 .A874.K9.";
            var deal = new DDS.Deal(ref cards);
            var state = new GameState(in deal, Suits.Spades, Seats.West, CardDeck.Instance[Suits.Hearts, Ranks.King], Bridge.Card.Null, Bridge.Card.Null);
            var result = ddsWrapper.BestCards(ref state);
            Assert.AreEqual(7, result[0].Tricks);
        }

        [TestMethod]
        public void BestCards_Profile()
        {
            string cards = "N:K95.QJT3.AKJ.AQJ JT42.87..K98765 AQ86.K652.86432. 73.A94.QT97.T432";
            var deal = new DDS.Deal(ref cards);

            var state = new GameState(in deal, Suits.Hearts, Seats.East, CardDeck.Instance[Suits.Diamonds, Ranks.Five], Bridge.Card.Null, Bridge.Card.Null);
            var result = ddsWrapper.BestCards(ref state);
            Assert.AreEqual(11, result[0].Tricks);
            Assert.AreEqual(5, result.Count);
        }

        [TestMethod]
        public void BestCard()
        {
            string cards = "N:K95.QJT3.AKJ.AQJ JT42.87..K98765 AQ86.K652.86432. 73.A94.QT97.T432";
            var deal = new DDS.Deal(ref cards);
            Debug.WriteLine(deal.ToPBN());
            var state = new GameState(in deal, Suits.Hearts, Seats.East, CardDeck.Instance[Suits.Diamonds, Ranks.Five], Bridge.Card.Null, Bridge.Card.Null);
            var result = ddsWrapper.BestCard(ref state);
            Assert.AreEqual(11, result[0].Tricks);
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void AllCards()
        {
            string cards = "N:K95.QJT3.AKJ.AQJ JT42.87.5.K98765 AQ86.K652.86432. 73.A94.QT97.T432";
            var deal = new DDS.Deal(ref cards);

            var state = new GameState(in deal, Suits.Hearts, Seats.East, Bridge.Card.Null, Bridge.Card.Null, Bridge.Card.Null);
            var result = ddsWrapper.AllCards(ref state);
            Assert.AreEqual(13, result.Count);
        }

        [TestMethod]
        public void CalcAllTables()
        {
            var deal1 = new DDS.Deal("N:954.QJT3.AJT.QJ6 KJT2.87.5.AK9875 AQ86.K9654.8643. 73.A2.KQ972.T432");
            var deal2 = new DDS.Deal("N:954.QJT3.AKJ.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.QT97.T432");
            var deal3 = new DDS.Deal("N:K95.QJT3.AKJ.AQJ JT42.87.5.K98765 AQ86.K652.86432. 73.A94.QT97.T432");

            ddsWrapper.ForgetPreviousBoard();
            //var result = ddsWrapper.PossibleTricks(new List<DDS.Deal> { deal1, deal2, deal3 }, []);
            var result =
                Profiler.Time(() =>
                {
                    return ddsWrapper.PossibleTricks(new List<DDS.Deal> { deal1, deal2, deal3 }, []);
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

            Assert.AreEqual(8, result[0][Seats.North, Suits.Spades]);
            Assert.AreEqual(11, result[2][Seats.North, Suits.Hearts]);
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
            Assert.AreEqual(4, result[Seats.North, Suits.NoTrump]);
            Assert.AreEqual(6, result[Seats.North, Suits.Spades]);
            Assert.AreEqual(2, result[Seats.North, Suits.Hearts]);
            Assert.AreEqual(5, result[Seats.North, Suits.Diamonds]);
            Assert.AreEqual(1, result[Seats.North, Suits.Clubs]);
            Assert.AreEqual(8, result[Seats.East, Suits.NoTrump]);
            Assert.AreEqual(7, result[Seats.East, Suits.Spades]);
            Assert.AreEqual(11, result[Seats.East, Suits.Hearts]);
            Assert.AreEqual(8, result[Seats.East, Suits.Diamonds]);
            Assert.AreEqual(12, result[Seats.East, Suits.Clubs]);
            Assert.AreEqual(4, result[Seats.South, Suits.NoTrump]);
            Assert.AreEqual(6, result[Seats.South, Suits.Spades]);
            Assert.AreEqual(2, result[Seats.South, Suits.Hearts]);
            Assert.AreEqual(5, result[Seats.South, Suits.Diamonds]);
            Assert.AreEqual(1, result[Seats.South, Suits.Clubs]);
            Assert.AreEqual(9, result[Seats.West, Suits.NoTrump]);
            Assert.AreEqual(7, result[Seats.West, Suits.Spades]);
            Assert.AreEqual(11, result[Seats.West, Suits.Hearts]);
            Assert.AreEqual(8, result[Seats.West, Suits.Diamonds]);
            Assert.AreEqual(12, result[Seats.West, Suits.Clubs]);
        }
    }
}