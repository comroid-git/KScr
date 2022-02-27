using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Lib.Store
{
    public sealed class ObjectStore
    {
        private readonly ConcurrentDictionary<string, ObjectRef?> _cache = new();

        public ObjectRef? this[Stack ctx, VariableContext varctx, string name]
        {
            get
            {
                foreach (string key in ctx.CreateKeys(varctx, name))
                    if (_cache.ContainsKey(key))
                        return _cache[key];
                return null;
            }
            set
            {
                foreach(string key in ctx.CreateKeys(varctx, name)){
                    if (value == null && _cache.TryRemove(key, out _))
                        return;
                    else _cache[key] = value;
                }
            }
        }

        public void ClearLocals(Stack ctx)
        {
            foreach (string localKey in _cache.Keys.Where(it => it.StartsWith(ctx.PrefixLocal)))
                if (!_cache.TryRemove(localKey, out _))
                    throw new InternalException("Unable to remove local variable " + localKey);
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }

    public class ObjectRef
    {
        public readonly IObject?[] Stack;

        public readonly IClassInstance Type;

        public ObjectRef(IClassInstance type, IObject? value) : this(type)
        {
            Value = value;
        }

        public ObjectRef(IClassInstance type, [Range(1, int.MaxValue)] int len = 1)
        {
            if (len < 1)
                throw new ArgumentOutOfRangeException(nameof(len), len, "Invalid ObjectRef size");

            Type = type;
            Stack = new IObject?[len];
        }

        public int Length => Stack.Length;
        public bool IsPipe => ReadAccessor != null || WriteAccessor != null;
        public virtual IEvaluable? ReadAccessor { get; set; }
        public virtual IEvaluable? WriteAccessor { get; set; }

        public IObject? this[RuntimeBase vm, int i]
        {
            get
            {
                if (ReadAccessor != null)
                {
                    var output = Numeric.Constant(vm, i);
                    ReadAccessor!.Evaluate(vm, null, ref output!);
                    return output.Value;
                }
                else
                {
                    return Stack[i];
                }
            }
            set
            {
                CheckTypeCompat(value!.Type);
                if (WriteAccessor != null)
                {
                    var output = new ObjectRef(Class.VoidType.DefaultInstance) { Value = value };
                    WriteAccessor!.Evaluate(vm, null, ref output!);
                }
                else
                {
                    InsertToStack(i, value);
                }
            }
        }

        public IObject? Value
        {
            get => Stack[0];
            set
            {
                CheckTypeCompat(value!.Type);
                InsertToStack(0, value);
            }
        }

        public override string ToString()
        {
            return Length > 1
                ? Type + "" + string.Join(",", Stack.Select(it => it?.ToString()))
                : Type + ": " + Value;
        }

        private void CheckTypeCompat(IClassInstance other)
        {
            if (!Type.CanHold(other))
                throw new InternalException("Invalid Type (" + other + ") assigned to reference of type " + Type);
        }

        private void InsertToStack(int i, IObject? value)
        {
            if (IsPipe)
                throw new InvalidOperationException("Cannot insert value inte pipe");
            Stack[i] = value;
        }

        public bool ToBool() => !(Value == IObject.Null // todo inspect
                                  || ((Value as Numeric)?.ImplicitlyFalse ?? false)
                                  || Value == null);

        public ObjectRef LogicalNot(RuntimeBase vm) =>
            ToBool() ? vm.ConstantFalse : vm.ConstantTrue;
    }
}