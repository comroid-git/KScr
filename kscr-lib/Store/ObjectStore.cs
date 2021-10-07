using System;
using System.Collections.Concurrent;
using System.Linq;
using KScr.Lib.Core;
using KScr.Lib.Exception;

namespace KScr.Lib.Store
{
    public sealed class ObjectStore
    {
        private readonly ConcurrentDictionary<string, ObjectRef?>
            cache = new ConcurrentDictionary<string, ObjectRef?>();

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

        private static string CreateKey(Context ctx, VariableContext varctx, string name)
        {
            return varctx switch
            {
                VariableContext.Local => ctx.PrefixLocal + name,
                VariableContext.This => ctx.PrefixThis + name,
                VariableContext.Absolute => name,
                _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
            };
        }

        public void ClearLocals(Context ctx)
        {
            foreach (var localKey in cache.Keys.Where(it => it.StartsWith(ctx.PrefixLocal)))
                if (!cache.TryRemove(localKey, out _))
                    throw new InternalException("Unable to remove local variable " + localKey);
        }

        public void Clear()
        {
            cache.Clear();
        }
    }

    public sealed class ObjectRef
    {
        private IObject? _value;

        public ObjectRef(TypeRef type) : this(type, null)
        {
        }

        public ObjectRef(TypeRef type, IObject? value)
        {
            Type = type;
            _value = value;
        }

        public TypeRef Type { get; }

        public IObject? Value
        {
            get => _value;
            set
            {
                var canHold = Type.CanHold(value?.Type);
                if (canHold)
                    _value = value;
                else
                    throw new InternalException("Invalid Type (" + value?.Type + ") assigned to reference of type " +
                                                Type);
            }
        }

        public override string ToString()
        {
            return Type + ": " + Value;
        }
    }
}