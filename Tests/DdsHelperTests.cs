using DDS;

namespace Tests
{
    [TestClass]
    public class DdsHelperTests
    {
        [TestMethod]
        public void PBN2Deal2PBN()
        {
            string deal = "N:954.QJT3.AJT.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.KQ97.T432";
            var dealBinary = new Deal(deal);
            var dealPBN = dealBinary.ToPBN();

            Assert.AreEqual(deal, dealPBN);
        }

        [TestMethod]
        public void Profiler_Time_EmptyBlock()
        {
            Profiler.Time(() =>
            {
            }, out var elapsedTime, 100000);
            Assert.IsTrue(elapsedTime.TotalMilliseconds > 0 && elapsedTime.TotalMilliseconds < 2, $"{elapsedTime.TotalMilliseconds}");
        }
    }
}