using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Perper.WebJobs.Extensions.Model
{
    internal static class FilterUtils
    {
        private static List<string>? _parseFieldName(Expression subexpression)
        {
            switch (subexpression)
            {
                case MemberExpression member:
                    var left = _parseFieldName(member.Expression);
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

        private static object? _parseFieldValue(Expression subexpression)
        {
            switch (subexpression)
            {
                case ConstantExpression constant:
                    return constant.Value;
                default:
                    // Ugly way to read FieldExpression-s (used by e.g. local variables)
                    return Expression.Lambda(subexpression).Compile().DynamicInvoke();
            }
        }

        private static void _addToFilter(Dictionary<string, object?> filter, Expression subexpression)
        {
            switch (subexpression)
            {
                case BinaryExpression binary:
                    switch (binary.NodeType)
                    {
                        case ExpressionType.And:
                            _addToFilter(filter, binary.Left);
                            _addToFilter(filter, binary.Right);
                            break;

                        case ExpressionType.Equal:
                            var fieldNameLeft = _parseFieldName(binary.Left);
                            var fieldNameRight = _parseFieldName(binary.Right);
                            if (fieldNameLeft != null && fieldNameRight != null)
                                throw new NotImplementedException("Support for comparing two fields  is not implemented yet.");
                            if (fieldNameLeft == null && fieldNameRight == null)
                                throw new NotImplementedException("Expected a field/property on one side of the equality test.");
                            var fieldName = string.Join(".", (fieldNameLeft != null ? fieldNameLeft : fieldNameRight)!);
                            var fieldValue = fieldNameLeft != null ? _parseFieldValue(binary.Right) : _parseFieldValue(binary.Left);
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

        public static Dictionary<string, object?> ConvertFilter<T>(Expression<Func<T, bool>> filter)
        {
            var result = new Dictionary<string, object?>();

            _addToFilter(result, filter.Body);

            return result;
        }
    }
}