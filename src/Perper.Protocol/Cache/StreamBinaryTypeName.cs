using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Cache
{
    public class StreamBinaryTypeName
    {
        public string StreamName { get; }
        public string DelegateName { get; }
        public DelegateType DelegateType { get; }

        public StreamBinaryTypeName(string streamName, string delegateName, DelegateType delegateType)
        {
            StreamName = streamName;
            DelegateName = delegateName;
            DelegateType = delegateType;
        }

        public override string ToString()
        {
            return $"{nameof(StreamBinaryTypeName)}_{StreamName}_{DelegateName}_{DelegateType}";
        }

        public static StreamBinaryTypeName Parse(string stringValue)
        {
            var pattern = $@"{nameof(StreamBinaryTypeName)}_(?'{nameof(StreamName)}'.*)_(?'{nameof(DelegateName)}'.*)_(?'{nameof(DelegateType)}'.*)";
            var match = Regex.Match(stringValue, pattern);

            var streamName = match.Groups[nameof(StreamName)].Value;
            var delegateName = match.Groups[nameof(DelegateName)].Value;
            var delegateType = Enum.Parse<DelegateType>(match.Groups[nameof(DelegateType)].Value);

            return new StreamBinaryTypeName(streamName, delegateName, delegateType);
        }
    }

    public enum DelegateType
    {
        Function,
        Action
    }
}