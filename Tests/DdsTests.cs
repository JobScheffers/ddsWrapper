using DDS;
using System.Diagnostics;

namespace Tests
{
    [TestClass]
    public class DdsTests
    {
        [TestMethod]
        public void SolveBoard()
        {
            string gamestate3 = "N:K95.QJT3.AKJ.AQJ JT42.87..K98765 AQ86.K652.86432. 73.A94.QT97.T432";

            var result =
                Profiler.Time(() =>
                {
                    return ddsWrapper.SolveBoard(new GameState { Trump = Suit.Hearts, TrickLeader = Hand.East, RemainingCards = new Deal(gamestate3), TrickCards = [new Card { Suit = Suit.Diamonds, Rank = Rank.Five }] });
                }, 100);
            Assert.AreEqual(11, result[0].Tricks);
        }

        //[TestMethod]
        public void CalcAllTables()
        {
            string gamestate1 = "N:954.QJT3.AJT.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.KQ97.T432";
            string gamestate2 = "N:954.QJT3.AKJ.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.QT97.T432";
            string gamestate3 = "N:K95.QJT3.AKJ.AQJ JT42.87.5.K98765 AQ86.K652.86432. 73.A94.QT97.T432";

            var result =
                Profiler.Time(() =>
                {
                    return ddsWrapper.PossibleTricks2(new List<Deal> { new Deal(gamestate1), new Deal(gamestate2), new Deal(gamestate3) }, new SuitCollection<bool>(new bool[5] { true, false, true, false, false }));
                });

            foreach (var deal in result)
            {
                Trace.WriteLine(gamestate1);
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
        }

        [TestMethod]
        public void CalcAllTablesPBN()
        {
            string gamestate1 = "N:954.QJT3.AJT.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.KQ97.T432";
            string gamestate2 = "N:954.QJT3.AKJ.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.QT97.T432";
            string gamestate3 = "N:K95.QJT3.AKJ.AQJ JT42.87.5.K98765 AQ86.K652.86432. 73.A94.QT97.T432";

            var result =
                Profiler.Time(() =>
                {
                    return ddsWrapper.PossibleTricks(new List<string> { gamestate1, gamestate2, gamestate3 });
                }, 10);

            foreach (var deal in result)
            {
                Trace.WriteLine(gamestate1);
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
            string deal = "N:K95.QJT3.AKJ.AQJ JT42.87.5.K98765 AQ86.K652.86432. 73.A94.QT97.T432";

            var result =
                Profiler.Time(() =>
                {
                    return ddsWrapper.PossibleTricks(deal);
                });

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

            Assert.AreEqual(11, result[Hand.North, Suit.Spades]);
            Assert.AreEqual(11, result[Hand.North, Suit.Hearts]);
            Assert.AreEqual(11, result[Hand.North, Suit.Diamonds]);
            Assert.AreEqual(7, result[Hand.North, Suit.Clubs]);
            Assert.AreEqual(11, result[Hand.North, Suit.NT]);
        }
    }
}