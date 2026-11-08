using Bridge;
using BridgeSolver;
using DDS;
using System.Diagnostics;

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
        public void BestCards()
        {
            var deal = new Deal("N:K95.QJT3.KQJ.AQJ JT42.87.5.K98765 AQ86.K652.86432. 73.A94.AT97.T432");

            var state = new GameState(in deal, 13, Suits.Hearts, Seats.North, Seats.East, Card.Null.Index, Card.Null.Index, Card.Null.Index);
            var result = BridgeSolverWrapper.BestCards(state, new List<Card>() { Card.Get(Suits.Diamonds, Ranks.Five), Card.Get(Suits.Spades, Ranks.Jack), Card.Get(Suits.Hearts, Ranks.Eight), Card.Get(Suits.Clubs, Ranks.Five) });
            Assert.AreEqual(3, result[0].Tricks);
            //Assert.AreEqual(5, result.Count);
        }

        [TestMethod]
        public void Analyzer_Dispose_DoesNotThrow()
        {
            var analyzer = new BridgeSolverWrapper.Analyzer();
            analyzer.Dispose();
            // Double-dispose must be safe (SafeHandle guarantees this).
            analyzer.Dispose();
        }

        [TestMethod]
        public void PossibleTricks_410Deals_5Trumps()
        {
            const bool printDeals = false;
            RandomGenerator.Instance.Repeatable(1);
            //var baseDeal = new Deal("N:954.QJT3.AJT.QJ6");
            var baseDeal = new Deal();
            var deals = new List<Deal>();
            const int numDeals = 410;
            for (int i = 0; i < numDeals; i++)
            {
                deals.Add(baseDeal.CompletedFromSeed(RandomGenerator.Instance.NextDealBigInteger()));
                if (printDeals) Trace.WriteLine(deals[i]);
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();

            TableResults needed = new TableResults();
            DdsEnum.ForEachHand(seat =>
            {
                DdsEnum.ForEachTrump(suit =>
                {
                    needed[seat, suit] = 1;
                });
            });

            var result = BridgeSolverWrapper.PossibleTricks(deals, needed, maxDuration: 5);

            sw.Stop();

            if (printDeals)
            {
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
            }

            Trace.WriteLine($"{result.Count} deals in {sw.Elapsed.TotalSeconds:F1}s; {sw.Elapsed.TotalSeconds / result.Count:F3}s per deal");
#if DEBUG
            //Assert.IsLessThan(12500, sw.ElapsedMilliseconds);
            Assert.IsGreaterThan(60, result.Count);
#endif  
        }
    }
}
