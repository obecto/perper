using System;

using Apache.Ignite.Core.Cache.Configuration;

namespace BasicSample
{
    public class SampleUserType
    {
        [QuerySqlField]
        public Guid Id { get; set; }
        public string[] MessagesBatch { get; set; }

        public SampleUserType(Guid id, string[] messagesBatch)
        {
            Id = id;
            MessagesBatch = messagesBatch;
        }
    }
}