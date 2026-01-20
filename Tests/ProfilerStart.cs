
using Tests;

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    public class Program
    {
        public static void Main()
        {
            //var tests = new SolveBoardBenchmark();
            //tests.SolveBoard_From_Multiple_Threads();

            var tests = new DdsTests();
            tests.CalcAllTables_100x5();

            //tests.SolveBoard3();
            //tests.CalcAllTables();
        }
    }
}