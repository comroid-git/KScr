using System;
using System.IO;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public enum ClassMemberType : byte
    {
        Method = 0x1,
        Property = 0x2
    }

    public interface IClassMember : IEvaluable, IModifierContainer
    {
        public Class Parent { get; }
        public string Name { get; }
        public string FullName { get; }
        public ClassMemberType MemberType { get; }
    }

    public abstract class AbstractClassMember : AbstractBytecode, IClassMember
    {
        private protected string _name;

        protected AbstractClassMember(Class parent, string name, MemberModifier modifier)
        {
            Parent = parent;
            _name = name;
            Modifier = modifier;
        }

        public Class Parent { get; }

        public virtual string Name => _name;

        public virtual string FullName => Parent.FullName + '.' + Name;
        public MemberModifier Modifier { get; protected set; }
        public abstract ClassMemberType MemberType { get; }

        public abstract void Evaluate(RuntimeBase vm, Stack stack, StackOutput copyFromStack = StackOutput.None);

        public override void Write(Stream stream)
        {
            stream.Write(new[] { (byte)MemberType });
            stream.Write(BitConverter.GetBytes((int)Modifier));
            byte[] buf = RuntimeBase.Encoding.GetBytes(Name);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
        }

        public override void Load(RuntimeBase vm, byte[] data)
        {
            var index = 0;
            _Load(data, ref index, out string name, out var modifier);
            _name = name;
            Modifier = modifier;
        }

        public static AbstractClassMember Read(RuntimeBase vm, Class parent, byte[] data, ref int index)
        {
            var type = _Load(data, ref index, out string name, out var modifier);
            AbstractClassMember member = type switch
            {
                ClassMemberType.Method => new Method(parent, name, null, modifier), // todo fixme
                ClassMemberType.Property => new Property(parent, name, null, modifier),
                _ => throw new ArgumentOutOfRangeException()
            };
            member.Load(vm, data, ref index);
            return member;
        }

        private static ClassMemberType _Load(byte[] data, ref int index, out string name, out MemberModifier modifier)
        {
            int len;
            var type = (ClassMemberType)data[index];
            index += 1;
            modifier = (MemberModifier)BitConverter.ToInt32(data, index);
            index += 4;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            name = RuntimeBase.Encoding.GetString(data, index, len);
            index += len;
            return type;
        }
    }
}