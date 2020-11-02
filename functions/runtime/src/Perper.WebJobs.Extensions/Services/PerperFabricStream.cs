using System;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Apache.Ignite.Core.Binary;
using System.Collections.Generic;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Protocol.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricStream : IPerperStream, IBinarizable
    {
        public string StreamName { get; private set; }

        public bool Subscribed { get; private set; }

        public Dictionary<string, object?> Filter { get; private set; }

        public string DeclaredDelegate { get; }

        public Type? DeclaredType { get; }

        private Func<Task>? _dispose;

        public PerperFabricStream(string streamName, bool subscribed = false, Dictionary<string, object?>? filter = null, string declaredDelegate = "", Type? declaredType = null, Func<Task>? dispose = null)
        {
            StreamName = streamName;
            Subscribed = subscribed;
            Filter = filter ?? new Dictionary<string, object?>();
            DeclaredDelegate = declaredDelegate;
            DeclaredType = declaredType;

            _dispose = dispose;
        }

        public IPerperStream Subscribe()
        {
            return new PerperFabricStream(StreamName, true, Filter);
        }

        IPerperStream IPerperStream.Filter<T>(Expression<Func<T, bool>> expression)
        {
            var newFilter = new Dictionary<string, object?>(Filter);
            string? parseFieldName(Expression subexpression)
            {
                switch (subexpression)
                {
                    case MemberExpression member:
                        var left = parseFieldName(member.Expression);
                        return left != null ? left + member.Member.Name : null;
                    case ParameterExpression parameter:
                        return parameter.Name == expression.Parameters[0].Name ? "" : null;
                    case ConstantExpression constant:
                        return null;
                    default:
                        throw new NotImplementedException("Support for " + subexpression.GetType() + " in IPerperStream.Filter is not implemented yet.");
                }
            }
            object? parseFieldValue(Expression subexpression)
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
            void addSubexpressionToFilter(Expression subexpression)
            {
                switch (subexpression)
                {
                    case BinaryExpression binary:
                        switch (binary.NodeType)
                        {
                            case ExpressionType.And:
                                addSubexpressionToFilter(binary.Left);
                                addSubexpressionToFilter(binary.Right);
                                break;

                            case ExpressionType.Equal:
                                var fieldNameLeft = parseFieldName(binary.Left);
                                var fieldNameRight = parseFieldName(binary.Right);
                                if (fieldNameLeft != null && fieldNameRight != null)
                                    throw new NotImplementedException("Support for comparing two fields in IPerperStream.Filter is not implemented yet.");
                                if (fieldNameLeft == null && fieldNameRight == null)
                                    throw new NotImplementedException("Expected a field/property on one side of the expression.");
                                var fieldName = (fieldNameLeft != null ? fieldNameLeft : fieldNameRight)!;
                                var fieldValue = fieldNameLeft != null ? parseFieldValue(binary.Right) : parseFieldValue(binary.Left);

                                if (fieldValue == null || JavaTypeMappingHelper.GetJavaTypeAsString(fieldValue.GetType()) == null)
                                {
                                    throw new NotImplementedException("Comparision with custom types in filters is not implemented yet.");
                                }

                                newFilter[fieldName] = fieldValue;
                                break;

                            default:
                                throw new NotImplementedException("Support for " + binary.NodeType + " in IPerperStream.Filter is not implemented yet.");
                        }
                        break;

                    default:
                        throw new NotImplementedException("Support for " + subexpression.GetType() + " in IPerperStream.Filter is not implemented yet.");
                }
            }

            addSubexpressionToFilter(expression.Body);

            return new PerperFabricStream(StreamName, Subscribed, newFilter);
        }

        public StreamParam AsStreamParam()
        {
            return new StreamParam(StreamName, Filter);
        }

        public ValueTask DisposeAsync()
        {
            return _dispose == null ? default : new ValueTask(_dispose());
        }

        public void WriteBinary(IBinaryWriter writer)
        {
            writer.WriteDictionary("filter", Filter);
            writer.WriteString("streamName", StreamName);
            writer.WriteBoolean("subscribed", Subscribed);
        }

        public void ReadBinary(IBinaryReader reader)
        {
            Filter = (Dictionary<string, object?>)reader.ReadDictionary("filter", s => new Dictionary<string, object?>(s));
            StreamName = reader.ReadString("streamName");
            Subscribed = reader.ReadBoolean("subscribed");
        }
    }
}