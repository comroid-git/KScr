using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;

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
                string? key = CreateKey(ctx, varctx, name);
                if (cache.ContainsKey(key))
                    return cache[key];
                return null;
            }
            set
            {
                string? key = CreateKey(ctx, varctx, name);
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
        private readonly IObject?[] _stack;

        public ObjectRef(IClassRef type, IObject? value) : this(type)
        {
            Value = value;
        }

        public ObjectRef(IClassRef type, [Range(1, int.MaxValue)] int len = 1)
        {
            if (len < 1)
                throw new ArgumentOutOfRangeException(nameof(len), len, "Invalid ObjectRef size");
            
            Type = type;
            _stack = new IObject?[len];
        }

        public readonly IClassRef Type;

        public int Length => _stack.Length;
        public IObject?[] Stack => _stack;
        public IObject? Value
        {
            get => Stack[0];
            set
            {
                bool canHold = Type.CanHold(value?.Type);
                if (canHold)
                    Stack[0] = value;
                else
                    throw new InternalException("Invalid Type (" + value?.Type + ") assigned to reference of type " +
                                                Type);
            }
        }

        public override string ToString() => Length > 1
            ? Type + "" + string.Join(",", _stack.Select(it => it?.ToString()))
            : Type + ": " + Value;
    }
}