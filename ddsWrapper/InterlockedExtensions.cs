using System.Threading;

namespace DDS
{
    internal static class InterlockedExtensions
    {
        /// <summary>
        /// Atomically set <paramref name="location"/> to the maximum of its current value and <paramref name="value"/>.
        /// Wait?free when current >= value; uses CAS loop otherwise.
        /// </summary>
        public static void Max(ref int location, int value)
        {
            int current;
            while (true)
            {
                current = Volatile.Read(ref location);
                if (current >= value) return;
                if (Interlocked.CompareExchange(ref location, value, current) == current) return;
            }
        }

        /// <summary>
        /// Same as <see cref="Max(ref int,int)"/> but for long counters.
        /// </summary>
        public static void Max(ref long location, long value)
        {
            long current;
            while (true)
            {
                current = Volatile.Read(ref location);
                if (current >= value) return;
                if (Interlocked.CompareExchange(ref location, value, current) == current) return;
            }
        }
    }
}