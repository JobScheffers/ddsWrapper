using Bridge;
using DDS;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Tests
{
    [TestClass]
    public class SolveBoardBenchmark
    {
//#if DEBUG
        [TestMethod]
//#endif
        public void RunSolveBoardBenchmark()
        {
            // Configuration
            const int warmupIterations = 10;
            const int measureIterations = 400;
            const int fixedThreadCount = 4;
            ddsWrapper.ForgetPreviousBoard();

            // Sample board (reuse any existing sample from your tests)
            var deal1 = new Deal("N:T9.2.732.T .JT5.T4.J4 54...A9862 .A874.K9.");
            var deal2 = new Deal("N:JT984.T7.AQ83.4 Q7532.82.97.832 K.AQJ53.KJ42.AK A6.K964.T65.Q96");
            var deal3 = new Deal("N:9..85432.QJ9 754.JT73.KT. J82.KQ6.QJ.6 AKQT63.5..8");

            Trace.WriteLine($"SolveBoard benchmark - warmup {warmupIterations}, measure {measureIterations}, parallelDegree {fixedThreadCount}");

            // Warmup
            for (int i = 0; i < warmupIterations; i++)
            {
                Work();
            }

            // Ensure a clean GC baseline for allocations measurement
            ddsWrapper.ForgetPreviousBoard();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Measure single-threaded (sequential)
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < measureIterations; i++)
            {
                Work();
            }
            sw.Stop();
            var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            Trace.WriteLine($"Sequential: total {sw.Elapsed.TotalMilliseconds:F2} ms, per call {sw.Elapsed.TotalMilliseconds / measureIterations:F4} ms, allocated {(allocatedAfter - allocatedBefore):N0} bytes (current thread)");

            // Small pause
            Task.Delay(200).Wait();

            // Measure parallel run using fixed threads
            ddsWrapper.ForgetPreviousBoard();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();

            var threads = new Thread[fixedThreadCount];
            int baseCount = measureIterations / fixedThreadCount;

            for (int t = 0; t < fixedThreadCount; t++)
            {
                int localCount = baseCount;
                threads[t] = new Thread(() =>
                {
                    for (int j = 0; j < localCount; j++)
                    {
                        Work();
                    }
                });
                threads[t].Start();
            }

            for (int t = 0; t < fixedThreadCount; t++)
            {
                threads[t].Join();
            }

            sw.Stop();
            allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            Trace.WriteLine($"Parallel ({fixedThreadCount}): total {sw.Elapsed.TotalMilliseconds:F2} ms, per-call avg {sw.Elapsed.TotalMilliseconds / measureIterations:F4} ms, allocated {(allocatedAfter - allocatedBefore):N0} bytes (current thread)");

            // Quick high-concurrency stress run (optional) using fixed threads
            ddsWrapper.ForgetPreviousBoard();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var stressIterations = Math.Max(1000, measureIterations * 5);
            sw.Restart();

            threads = new Thread[fixedThreadCount];
            baseCount = stressIterations / fixedThreadCount;

            for (int t = 0; t < fixedThreadCount; t++)
            {
                int localCount = baseCount;
                threads[t] = new Thread(() =>
                {
                    for (int j = 0; j < localCount; j++)
                    {
                        Work();
                    }
                });
                threads[t].Start();
            }

            for (int t = 0; t < fixedThreadCount; t++)
            {
                threads[t].Join();
            }

            sw.Stop();
            Trace.WriteLine($"Stress: {stressIterations} calls, total {sw.Elapsed.TotalMilliseconds:F0} ms, per-call avg {sw.Elapsed.TotalMilliseconds / stressIterations:F4} ms");

            // Keep test green
            Assert.IsTrue(true);

            void Work()
            {
                var state1 = new GameState(in deal1, Suits.Spades, Seats.West, CardDeck.Instance[Suits.Hearts, Ranks.King], Bridge.Card.Null, Bridge.Card.Null);
                var result1 = ddsWrapper.BestCards(state1);
                var state2 = new GameState(in deal2, Suits.Hearts, Seats.South);
                var result2 = ddsWrapper.BestCards(state2);
                var state3 = new GameState(in deal3, Suits.Spades, Seats.West, CardDeck.Instance[Suits.Clubs, Ranks.Seven], Bridge.Card.Null, Bridge.Card.Null);
                var result3 = ddsWrapper.BestCards(in state3);
            }
        }

//#if DEBUG
        [TestMethod]
//#endif
        public void InspectFutureTricksViaReflection()
        {
            var asm = typeof(ddsWrapper).Assembly;
            var ftType = asm.GetType("DDS.FutureTricks", throwOnError: true);
            Trace.WriteLine($"Found type: {ftType.FullName}, IsPublic={ftType.IsPublic}, IsValueType={ftType.IsValueType}");

            // Get managed Marshal size
            var marshalSize = Marshal.SizeOf(ftType);
            Trace.WriteLine($"Marshal.SizeOf(FutureTricks) = {marshalSize} bytes");

            // If you need sizeof at compile time you'll need InternalsVisibleTo + unsafe sizeof(FutureTricks)
            // You can still make a P/Invoke call that returns sizeof in native code, or expose a managed accessor.
            Assert.IsTrue(marshalSize > 0);
        }
    }
}
