using System;
using System.Globalization;
using System.Reflection;
using Microsoft.Azure.WebJobs;

namespace Perper.WebJobs.Extensions
{
    public static class FunctionNameHelper
    {
        // From https://github.com/Azure/azure-webjobs-sdk/blob/master/src/Microsoft.Azure.WebJobs.Host/Indexers/MethodInfoExtensions.cs#L12
        public static string GetFullName(this MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            return String.Format(CultureInfo.InvariantCulture, "{0}.{1}", methodInfo.DeclaringType.FullName, methodInfo.Name);
        }

        public static MethodInfo GetFunctionMethod(this Type type)
        {
            MethodInfo foundMethod = null;

            foreach (var method in type.GetMethods())
            {
                if (method.GetCustomAttribute<FunctionNameAttribute>() != null)
                {
                    if (foundMethod != null)
                    {
                        throw new AmbiguousMatchException("Expected a type with only one [FunctionName] method.");
                    }

                    foundMethod = method;
                }
            }

            return foundMethod;
        }
    }
}