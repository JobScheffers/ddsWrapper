using BridgeSolver;

namespace Tests
{
    [TestClass]
    public class BridgeSolverTests
    {
        // N:JT984.T7.AQ83.4 Q7532.82.97.832 K.AQJ53.KJ42.AK A6.K964.T65.Q96
        // Same deal used in DdsTests so results can be cross-checked.
        private const string Deal2 = "N:JT984.T7.AQ83.4 Q7532.82.97.832 K.AQJ53.KJ42.AK A6.K964.T65.Q96";

        [TestMethod]
        public void GetVersion_ReturnsNonEmptyString()
        {
            var version = BridgeSolverWrapper.GetVersion();
            Assert.IsFalse(string.IsNullOrWhiteSpace(version), $"Expected a version string, got: '{version}'");
        }

        [TestMethod]
        public void Solve_KnownDeal_ReturnsExpectedTricks()
        {
            // NS declare 4S: South (S) is declarer, West (W) leads.
            int tricks = BridgeSolverWrapper.Solve(Deal2, "S", "W");
            // NS makes 8 tricks in spades double-dummy with West on lead.
            Assert.AreEqual(8, tricks, $"Expected 8 tricks for NS in spades, got {tricks}");
        }

        [TestMethod]
        public void DdTableTricks_KnownDeal_Returns20Bytes()
        {
            var tricks = BridgeSolverWrapper.DdTableTricks(Deal2);

            // Spot-check: North declaring spades (seat=0 N, strain=3 S) should be 8 tricks.
            // tricks[seat * 5 + strain] — seats: N=0,E=1,S=2,W=3; strains: C=0,D=1,H=2,S=3,NT=4
            Assert.AreEqual(8, tricks[Bridge.Seats.North, Bridge.Suits.Spades], $"Expected N to make 8 tricks in S, got {tricks[Bridge.Seats.North, Bridge.Suits.Spades]}");
        }

        [TestMethod]
        public void Par_KnownDeal_ReturnsJson()
        {
            string json = BridgeSolverWrapper.Par(Deal2, "None");
            Assert.IsFalse(string.IsNullOrWhiteSpace(json));
            // Should not be an error response.
            Assert.IsFalse(json.Contains("\"error\""), $"Par returned an error: {json}");
        }

        [TestMethod]
        public void Analyzer_Play_ReturnsJson()
        {
            // A minimal play trace: NS in 4S, West leads SA (which East doesn't have — just checking the
            // round-trip; the library returns an error JSON for invalid input, never null).
            const string request = """
                {
                    "dealstr": "N:JT984.T7.AQ83.4 Q7532.82.97.832 K.AQJ53.KJ42.AK A6.K964.T65.Q96",
                    "trump": "S",
                    "declarer": "S",
                    "leader": "W",
                    "plays": []
                }
                """;

            using var analyzer = new BridgeSolverWrapper.Analyzer();
            string json = analyzer.Play(request);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json));
        }

        [TestMethod]
        public void Analyzer_Dispose_DoesNotThrow()
        {
            var analyzer = new BridgeSolverWrapper.Analyzer();
            analyzer.Dispose();
            // Double-dispose must be safe (SafeHandle guarantees this).
            analyzer.Dispose();
        }
    }
}
