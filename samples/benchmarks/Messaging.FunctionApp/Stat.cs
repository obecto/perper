using System.Threading;

namespace Messaging.FunctionApp
{
    public class Stat
    {
        private long Value = 0;

        public long Max { get; set; } = 0;

        public Stat(long value = 0, long max = -1)
        {
            Value = value;
            Max = max;
        }

        public bool Increment() => Interlocked.Increment(ref Value) < Max || Max == -1;

        public long Get() => Interlocked.Read(ref Value);

        public bool IsMax() => Interlocked.Read(ref Value) >= Max && Max != -1;

        public class Reader
        {
            private readonly Stat Stat;
            private long LastValue;

            public Reader(Stat stat)
            {
                Stat = stat;
                LastValue = Stat.Value;
            }

            public long Advance()
            {
                var value = Stat.Get();

                var result = value - LastValue;
                LastValue = value;

                return result;
            }
        }

        public Reader Read() => new Reader(this);
    }
}