using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Fake
{
    public static class FakeConfiguration
    {
        public static bool SerializeObjects { get; set; } = true;

        private static readonly PerperBinarySerializer? Serializer = new PerperBinarySerializer(null);

        internal static object? Serialize<T>(T value)
        {
            if (SerializeObjects)
            {
                return Serializer!.Serialize(value);
            }
            return value;
        }

        internal static T Deserialize<T>(object? serialized)
        {
            if (SerializeObjects)
            {
                return (T)Serializer!.Deserialize(serialized, typeof(T))!;
            }
            return (T)serialized!;
        }
    }
}