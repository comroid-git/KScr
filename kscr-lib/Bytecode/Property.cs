using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public sealed class Property : AbstractClassMember
    {
        public bool Gettable;
        public ExecutableCode? Getter;
        public bool Settable;
        public ExecutableCode? Setter;

        public Property(Class parent, string name, ITypeInfo returnType, MemberModifier modifier) : base(parent, name,
            modifier)
        {
            ReturnType = returnType;
        }

        public override string FullName => Parent.FullName + '.' + Name + ": " + ReturnType.FullName;

        public override ClassMemberType Type => ClassMemberType.Field;
        public ITypeInfo ReturnType { get; private set; }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => new[] { Getter, Setter }
            .Where(x => x != null).Cast<ExecutableCode>();

        public override IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0)
        {
            if (alt == 0 && Getter != null)
                return Getter.Evaluate(vm, ref state, ref rev);
            if (alt == 0 && Setter != null)
                return Setter.Evaluate(vm, ref state, ref rev);
            var value = rev!.Value;
            string subKey = CreateSubKey(value.GetKey());
            var clsInst = value.Type;
            if (vm[VariableContext.Absolute, subKey] == null)
                rev = vm[VariableContext.Absolute, subKey] = new ObjectRef(ReturnType.ResolveType(clsInst));
            else rev = vm[VariableContext.Absolute, CreateSubKey(rev.Value.GetKey())];
            return null;
        }

        private string CreateSubKey(string ownerKey)
        {
            return $"property-{ownerKey}.{Name}";
        }

        public override void Write(Stream stream)
        {
            base.Write(stream);
            byte[] buf = RuntimeBase.Encoding.GetBytes(ReturnType.FullName);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
            stream.Write(BitConverter.GetBytes(Getter != null));
            Getter?.Write(stream);
            stream.Write(BitConverter.GetBytes(Setter != null));
            Setter?.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int i)
        {
            base.Load(vm, data, ref i);
            var len = BitConverter.ToInt32(data, i);
            i += 4;
            ReturnType = vm.FindType(RuntimeBase.Encoding.GetString(data, i, len))!;
            i += len;
            Gettable = BitConverter.ToBoolean(data, i);
            i += 1;
            if (Gettable)
            {
                Getter = new ExecutableCode();
                Getter.Load(vm, data, ref i);
            }

            Settable = BitConverter.ToBoolean(data, i);
            i += 1;
            if (Settable)
            {
                Setter = new ExecutableCode();
                Setter.Load(vm, data, ref i);
            }
        }

        public new static Property Read(RuntimeBase vm, Class parent, byte[] data, ref int i)
        {
            return (AbstractClassMember.Read(vm, parent, data, ref i) as Property)!;
        }
    }
}