using BenchmarkDotNet.Running;

namespace BenchmarkSuite1
{
    internal class Program
    {
        static void Main(string[] _)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
