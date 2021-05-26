using System;
using System.Dynamic;
using Apache.Ignite.Core.Binary;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperDynamicObject : DynamicObject
    {
        private IBinaryObjectBuilder? _binaryObjectBuilder;
        private IBinaryObject? _binaryObject;

        public IBinaryObjectBuilder BinaryObjectBuilder
        {
            get
            {
                if (_binaryObjectBuilder == null)
                {
                    _binaryObjectBuilder = _binaryObject!.ToBuilder();
                    _binaryObject = null;
                }
                return _binaryObjectBuilder;
            }
        }
        public IBinaryObject BinaryObject
        {
            get
            {
                if (_binaryObject == null)
                {
                    _binaryObject = _binaryObjectBuilder!.Build();
                    _binaryObjectBuilder = null;
                }
                return _binaryObject;
            }
        }

        public PerperDynamicObject(IBinaryObject binaryObject)
        {
            _binaryObject = binaryObject;
        }

        public PerperDynamicObject(IBinaryObjectBuilder binaryObjectBuilder)
        {
            _binaryObjectBuilder = binaryObjectBuilder;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (BinaryObject.HasField(binder.Name))
            {
                result = BinaryObject.GetField<object?>(binder.Name);
                if (result is IBinaryObject binaryObject)
                {
                    if (Guid.TryParse(binaryObject.GetBinaryType().TypeName, out _))
                    {
                        // Anonymous type
                        result = new PerperDynamicObject(binaryObject);
                    }
                    else
                    {
                        result = binaryObject.Deserialize<object?>();
                    }
                }
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            BinaryObjectBuilder.SetField(binder.Name, value);
            return true;
        }

        public override string ToString() => BinaryObject.ToString()!;
    }
}