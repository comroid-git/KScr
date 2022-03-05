using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public sealed class Field : AbstractClassMember
    {
        public ExecutableCode Getter = null!;
        public ExecutableCode Setter = null!;

        public Field(Class parent, string name, ITypeInfo returnType, MemberModifier modifier) : base(parent, name, modifier)
        {
            ReturnType = returnType;
        }

        public override ClassMemberType Type => ClassMemberType.Field;
        public ITypeInfo ReturnType { get; }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => new[] { Getter, Setter };

        public override IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0)
        {
            return (alt == 0 ? Getter : Setter).Evaluate(vm, ref state, ref rev);
        }

        public override void Write(Stream stream)
        {
            base.Write(stream);
            byte[] buf = RuntimeBase.Encoding.GetBytes(ReturnType.FullName);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
            Getter.Write(stream);
            Setter?.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int i)
        {
            base.Load(vm, data, ref i);
        }

        public new static Field Read(RuntimeBase vm, Class parent, byte[] data, ref int i)
        {
            return (AbstractClassMember.Read(vm, parent, data, ref i) as Field)!;
        }
    }
}