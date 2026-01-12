using Bridge;
using DDS;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Tests
{
    [TestClass]
    public class SolveBoardBenchmark
    {
        [TestMethod]
        public void RunSolveBoardBenchmark()
        {
            // Configuration
            const int warmupIterations = 10;
            const int measureIterations = 4000;
            var parallelDegree = Environment.ProcessorCount - 1;
            ddsWrapper.ForgetPreviousBoard();

            // Sample board (reuse any existing sample from your tests)
            var deal1 = new Deal("N:T9.2.732.T .JT5.T4.J4 54...A9862 .A874.K9.");
            var deal2 = new Deal("N:JT984.T7.AQ83.4 Q7532.82.97.832 K.AQJ53.KJ42.AK A6.K964.T65.Q96");
            var deal3 = new Deal("N:9..85432.QJ9 754.JT73.KT. J82.KQ6.QJ.6 AKQT63.5..8");

            Trace.WriteLine($"SolveBoard benchmark - warmup {warmupIterations}, measure {measureIterations}, parallelDegree {parallelDegree}");

            // Warmup
            for (int i = 0; i < warmupIterations; i++)
            {
                Work();
            }

            // Ensure a clean GC baseline for allocations measurement
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

            // Measure parallel run (Parallel.For)
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            Parallel.For(0, measureIterations, new ParallelOptions { MaxDegreeOfParallelism = parallelDegree }, i =>
            {
                Work();
            });
            sw.Stop();
            allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            Trace.WriteLine($"Parallel ({parallelDegree}): total {sw.Elapsed.TotalMilliseconds:F2} ms, per-call avg {sw.Elapsed.TotalMilliseconds / measureIterations:F4} ms, allocated {(allocatedAfter - allocatedBefore):N0} bytes (current thread)");

            // Quick high-concurrency stress run (optional)
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var stressIterations = Math.Max(1000, measureIterations * 5);
            sw.Restart();
            Parallel.For(0, stressIterations, new ParallelOptions { MaxDegreeOfParallelism = parallelDegree }, i =>
            {
                Work();
            });
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

        //[TestMethod]
        //public void InspectFutureTricksLayoutAndCall()
        //{
        //    // Print managed sizes
        //    Trace.WriteLine($"Marshal.SizeOf<FutureTricks>() = {Marshal.SizeOf<FutureTricks>()} bytes");
        //    unsafe
        //    {
        //        Trace.WriteLine($"sizeof(FutureTricks) = {sizeof(FutureTricks)} bytes");
        //    }

        //    // If you changed 'deal' to blittable, print it too
        //    Trace.WriteLine($"Marshal.SizeOf<deal>() = {Marshal.SizeOf<deal>()}");

        //    // DDS info from the native lib
        //    try
        //    {
        //        var info = default(DDSInfo);
        //        ddsImports.GetDDSInfo(ref info); // safe to call, exists in ddsImports
        //        Trace.WriteLine($"DDS native noOfThreads={info.noOfThreads}, threading={info.threading}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.WriteLine($"GetDDSInfo failed: {ex.Message}");
        //    }

        //    // Make a minimal SolveBoard call to verify no corruption and get hresult
        //    var deal = new Deal("N:T9.2.732.T .JT5.T4.J4 54...A9862 .A874.K9.");
        //    var state = new GameState(in deal, Suits.Spades, Seats.West, CardDeck.Instance[Suits.Hearts, Ranks.King], Bridge.Card.Null, Bridge.Card.Null);

        //    // call BestCards (wrap in try/catch to capture errors)
        //    try
        //    {
        //        var sw = Stopwatch.StartNew();
        //        var result = ddsWrapper.BestCards(state);
        //        sw.Stop();
        //        Trace.WriteLine($"BestCards returned {result?.Count ?? 0} entries in {sw.Elapsed.TotalMilliseconds:F2} ms");
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.WriteLine($"BestCards threw: {ex}");
        //        throw;
        //    }
        //}

        [TestMethod]
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
