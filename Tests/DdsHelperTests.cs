using Bridge;
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
            var dealBinary = new DDS.Deal(deal);
            var dealPBN = dealBinary.ToPBN();

            Assert.AreEqual(deal, dealPBN);
        }

        [TestMethod]
        public void ErrorMessage()
        {
            var error = ddsWrapper.Error(-15);
        }
    }
}