
using Tests;

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    public class Program
    {
        public static void Main()
        {
            var tests = new DdsTests();
            tests.SolveBoard();
        }
    }
}