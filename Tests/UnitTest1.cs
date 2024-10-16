using Bridge;
using DDS;
using System.Diagnostics;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        //[TestMethod]
        public void CalcAllTables()
        {
            string gamestate1 = "N:954.QJT3.AJT.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.KQ97.T432";
            string gamestate2 = "N:954.QJT3.AKJ.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.QT97.T432";
            string gamestate3 = "N:K95.QJT3.AKJ.AQJ JT42.87.5.K98765 AQ86.K652.86432. 73.A94.QT97.T432";

            var result3 = ddsWrapper.PossibleTricks2(new List<Deal> { new Deal(gamestate1), new Deal(gamestate2), new Deal(gamestate3) }, new SuitCollection<bool>(new bool[5] { true, false, true, false, false }));
            foreach (var deal in result3)
            {
                Trace.WriteLine(gamestate1);
                Trace.WriteLine("       C  D  H  S  NT");
                SeatsExtensions.ForEachSeat(seat =>
                {
                    Trace.Write($"{seat.ToString().PadRight(5)}");
                    SuitHelper.ForEachTrump(suit =>
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

            var result2 = ddsWrapper.PossibleTricks(new List<string> { gamestate1, gamestate2, gamestate3 });
            foreach (var deal in result2)
            {
                Trace.WriteLine(gamestate1);
                Trace.WriteLine("       C  D  H  S  NT");
                SeatsExtensions.ForEachSeat(seat =>
                {
                    Trace.Write($"{seat.ToString().PadRight(5)}");
                    SuitHelper.ForEachTrump(suit =>
                    {
                        Trace.Write($" {deal[seat, suit]:00}");
                    });
                    Trace.WriteLine($"");
                });
            }

            Assert.AreEqual(8, result2[0][Seats.North, Suits.Spades]);
            Assert.AreEqual(11, result2[2][Seats.North, Suits.Hearts]);
        }

        [TestMethod]
        public void CalcDDtablePBN()
        {
            string deal = "N:954.QJT3.AJT.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.KQ97.T432";

            var result = ddsWrapper.PossibleTricks(deal);

            Trace.WriteLine(deal);
            Trace.WriteLine("       C  D  H  S  NT");
            SeatsExtensions.ForEachSeat(seat =>
            {
                Trace.Write($"{seat.ToString().PadRight(5)}");
                SuitHelper.ForEachTrump(suit =>
                {
                    Trace.Write($" {result[seat, suit]:00}");
                });
                Trace.WriteLine($"");
            });

            Assert.AreEqual(8, result[Seats.North, Suits.Spades]);
        }
    }
}