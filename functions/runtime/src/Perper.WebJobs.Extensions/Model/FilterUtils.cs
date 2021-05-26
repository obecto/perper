using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Perper.WebJobs.Extensions.Model
{
    internal static class FilterUtils
    {
        public static Dictionary<string, object?> ConvertFilter<T>(Expression<Func<T, bool>> filter)
        {
            var result = new Dictionary<string, object?>();

            AddToFilter(result, filter.Body);

            return result;
        }

        private static void AddToFilter(Dictionary<string, object?> filter, Expression subexpression)
        {
            switch (subexpression)
            {
                case BinaryExpression binary:
                    switch (binary.NodeType)
                    {
                        case ExpressionType.And:
                            AddToFilter(filter, binary.Left);
                            AddToFilter(filter, binary.Right);
                            break;

                        case ExpressionType.Equal:
                            var fieldNameLeft = ParseFieldName(binary.Left);
                            var fieldNameRight = ParseFieldName(binary.Right);
                            if (fieldNameLeft != null && fieldNameRight != null)
                                throw new NotImplementedException("Support for comparing two fields  is not implemented yet.");
                            if (fieldNameLeft == null && fieldNameRight == null)
                                throw new NotImplementedException("Expected a field/property on one side of the equality test.");
                            var fieldName = string.Join(".", (fieldNameLeft ?? fieldNameRight)!);
                            var fieldValue = fieldNameLeft != null ? ParseFieldValue(binary.Right) : ParseFieldValue(binary.Left);
                            filter.Add(fieldName, fieldValue);
                            break;

                        default:
                            throw new NotImplementedException("Support for " + binary.NodeType + " in filters is not implemented yet.");
                    }
                    break;

                default:
                    throw new NotImplementedException("Support for " + subexpression.GetType() + " in filters is not implemented yet.");
            }
        }

        private static List<string>? ParseFieldName(Expression subexpression)
        {
            switch (subexpression)
            {
                case MemberExpression member:
                    var left = ParseFieldName(member.Expression);
                    left?.Add(member.Member.Name);
                    return left;
                case ParameterExpression _:
                    return new List<string>(); // Assuming there is only one parameter
                case ConstantExpression _:
                    return null;
                default:
                    throw new NotImplementedException("Support for " + subexpression.GetType() + " in IPerperStream.Filter is not implemented yet.");
            }
        }

        private static object? ParseFieldValue(Expression subexpression) => subexpression switch
        {
            ConstantExpression constant => constant.Value,
            _ => Expression.Lambda(subexpression).Compile().DynamicInvoke(),// Ugly way to read FieldExpression-s (used by e.g. local variables)
        };
    }
}