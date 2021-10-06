using System;
using System.Collections.Concurrent;
using KScr.Lib.Core;

namespace KScr.Lib.VM
{
    public sealed class ObjectStore
    {
        private ConcurrentDictionary<string, ObjectRef?> cache = new ConcurrentDictionary<string, ObjectRef?>();

        public ObjectRef? this[Context ctx, VariableContext varctx, string name]
        {
            get
            {
                var key = CreateKey(ctx, varctx, name);
                if (cache.ContainsKey(key))
                    return cache[key];
                return null;
            }
            set 
            {
                var key = CreateKey(ctx, varctx, name);
                cache[key] = value;
            } 
        }

        private static string CreateKey(Context ctx, VariableContext varctx, string name) => varctx switch
        {
            VariableContext.Local => ctx.Local + Context.Delimiter + name,
            VariableContext.This => ctx.This + Context.Delimiter + name,
            VariableContext.Relative => ctx.Relative + Context.Delimiter + name,
            VariableContext.Absolute => name,
            _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
        };
        
        public void Clear() => cache.Clear();
    }

    public sealed class ObjectRef
    {
        private readonly TypeRef _type;
        private IObject? _value;

        public ObjectRef(TypeRef type) : this(type, null)
        {
        }

        public ObjectRef(TypeRef type, IObject? value)
        {
            _type = type;
            _value = value;
        }

        public TypeRef Type => _type;
        public IObject? Value
        {
            get => _value;
            set
            {
                bool canHold = Type.CanHold(value?.Type);
                if (canHold)
                    _value = value;
                else throw new Exception("Invalid Type ("+value?.Type+") assigned to reference of type " + Type);
            }
        }

        public override string ToString() => _type + ": " + Value;
    }
}