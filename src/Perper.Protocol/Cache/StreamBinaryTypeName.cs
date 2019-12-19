using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Cache
{
    public class StreamBinaryTypeName
    {
        public string DelegateName { get; }
        public DelegateType DelegateType { get; }

        public StreamBinaryTypeName(string delegateName, DelegateType delegateType)
        {
            DelegateName = delegateName;
            DelegateType = delegateType;
        }

        public override string ToString()
        {
            return $"{nameof(StreamBinaryTypeName)}<{DelegateName},{DelegateType}>";
        }

        public static StreamBinaryTypeName Parse(string stringValue)
        {
            var pattern = $@"{nameof(StreamBinaryTypeName)}<(?'{nameof(DelegateName)}'.*),(?'{nameof(DelegateType)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            
            var name = match.Groups[nameof(DelegateName)].Value; 
            var delegateType = Enum.Parse<DelegateType>(match.Groups[nameof(DelegateType)].Value);
            
            return new StreamBinaryTypeName(name, delegateType);
        }
    }

    public enum DelegateType
    {
        Function,
        Action
    }
}