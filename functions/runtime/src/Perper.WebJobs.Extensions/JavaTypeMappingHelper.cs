using System;
using System.Collections.Generic;
using System.Text;

namespace Perper.WebJobs.Extensions
{
    public static class JavaTypeMappingHelper
    {
        private static Dictionary<Type, string> mappings = new Dictionary<Type, string>()
        {
            {typeof (bool), "java.lang.Boolean"},
            {typeof (byte), "java.lang.Byte"},
            {typeof (sbyte), "java.lang.Byte"},
            {typeof (short), "java.lang.Short"},
            {typeof (ushort), "java.lang.Short"},
            {typeof (char), "java.lang.Character"},
            {typeof (int), "java.lang.Integer"},
            {typeof (uint), "java.lang.Integer"},
            {typeof (long), "java.lang.Long"},
            {typeof (ulong), "java.lang.Long"},
            {typeof (float), "java.lang.Float"},
            {typeof (double), "java.lang.Double"},
            {typeof (string), "java.lang.String"},
            {typeof (decimal), "java.math.BigDecimal"},
            {typeof (Guid), "java.util.UUID"},
            {typeof (DateTime), "java.sql.Timestamp"}
        };

        public static string GetJavaTypeAsString(Type type)
        {
            if (mappings.ContainsKey(type))
            {
                return mappings[type];
            }
            else
            {
                return null;
            }
        }
    }
}