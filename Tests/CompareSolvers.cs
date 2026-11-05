using BridgeSolver;
using DDS;
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

    }
}
