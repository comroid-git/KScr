using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using IEvaluable = KScr.Lib.Model.IEvaluable;

namespace KScr.Lib.Store
{
    public delegate IEnumerable<string> ObjectStoreKeyGenerator(VariableContext varctx, string name);
    
    public sealed class ObjectStore
    {
        private readonly ConcurrentDictionary<string, ObjectRef?> _cache = new();

        public ObjectRef? this[ObjectStoreKeyGenerator keygen, VariableContext varctx, string name]
        {
            get
            {
                foreach (string key in keygen(varctx, name))
                    if (_cache.ContainsKey(key))
                        return _cache[key];
                return null;
            }
            set
            {
                foreach (string key in keygen(varctx, name))
                    if (value == null && _cache.TryRemove(key, out _))
                        return;
                    else _cache[key] = value;
            }
        }

        public void ClearLocals(Stack ctx)
        {
            foreach (string localKey in _cache.Keys.Where(it => it.StartsWith(ctx.PrefixLocal)))
                if (!_cache.TryRemove(localKey, out _))
                    throw new FatalException("Unable to remove local variable " + localKey);
        }

        public bool Remove(string key)
        {
            return _cache.TryRemove(key, out _);
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }

    public interface IObjectRef
    {
        int Length { get; }
        bool IsPipe { get; }
        Stack ReadValue(RuntimeBase vm, Stack stack, IObject @from);
        Stack WriteValue(RuntimeBase vm, Stack stack, IObject to);
        IEvaluable? ReadAccessor { get; set; }
        IEvaluable? WriteAccessor { get; set; }
        IObject Value { get; set; }
        IClassInstance Type { get; }
        IObject this[RuntimeBase vm, Stack stack, int i] { get; set; }
    }

    public static class IObjectRefExt
    {

        public static bool ToBool(this IObjectRef it)
        {
            return it != null 
                   && !(it.Value == IObject.Null // todo inspect
                     || ((it.Value as Numeric)?.ImplicitlyFalse ?? false)
                     || it.Value == null);
        }

        public static IObjectRef LogicalNot(this IObjectRef it, RuntimeBase vm)
        {
            return ToBool(it) ? vm.ConstantFalse : vm.ConstantTrue;
        }
    }

    public class ObjectRef : IObjectRef
    {
        public readonly IObject?[] Refs;

        public IClassInstance Type { get; }

        public ObjectRef(IClassInstance type, IObject value) : this(type)
        {
            Value = value;
        }

        public ObjectRef(IClassInstance type, [Range(1, int.MaxValue)] int len = 1)
        {
            if (len < 1)
                len = 1;
            Type = type;
            if (Type == null)
            {
                throw new NullReferenceException("type cannot be null");
            }

            Refs = new IObject?[len];
        }

        public int Length => Refs.Length;
        public bool IsPipe => ReadAccessor != null || WriteAccessor != null;
        public Stack ReadValue(RuntimeBase vm, Stack stack, IObject @from) => ReadAccessor!.Evaluate(vm, stack);
        public Stack WriteValue(RuntimeBase vm, Stack stack, IObject to) => WriteAccessor!.Evaluate(vm, stack);

        public virtual IEvaluable? ReadAccessor { get; set; }
        public virtual IEvaluable? WriteAccessor { get; set; }

        public IObject this[RuntimeBase vm, Stack stack, int i]
        {
            get
            {
                if (ReadAccessor != null)
                {
                    var output = Numeric.Constant(vm, i);
                    ReadAccessor!.Evaluate(vm, stack);
                    return output.Value;
                }
                else
                {
                    return Refs[i] ?? IObject.Null;
                }
            }
            set
            {
                CheckTypeCompat(value.Type);
                if (WriteAccessor != null)
                {
                    var output = new ObjectRef(Class.VoidType.DefaultInstance) { Value = value };
                    WriteAccessor!.Evaluate(vm, stack);
                }
                else
                {
                    InsertToStack(i, value);
                }
            }
        }

        public IObject Value
        {
            get => Refs[0] ?? IObject.Null;
            set
            {
                CheckTypeCompat(value.Type);
                InsertToStack(0, value);
            }
        }

        public override string ToString()
        {
            return Length > 1
                ? Type + "" + string.Join(",", Refs.Select(it => it?.ToString()))
                : Type + ": " + Value;
        }

        private void CheckTypeCompat(IClassInstance other)
        {
            if (!Type.CanHold(other))
                throw new FatalException("Invalid Type (" + other + ") assigned to reference of type " + Type);
        }

        private void InsertToStack(int i, IObject? value)
        {
            if (IsPipe)
                throw new InvalidOperationException("Cannot insert value inte pipe");
            Refs[i] = value;
        }
    }
}