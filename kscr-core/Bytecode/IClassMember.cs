using System;
using System.IO;
using KScr.Core.Model;
using KScr.Core.Store;
using KScr.Core.Util;

namespace KScr.Core.Bytecode
{
    public enum ClassMemberType : byte
    {
        Method = 0x1,
        Property = 0x2,
        Class = 0x4
    }

    public interface IClassMember : IEvaluable, IModifierContainer
    {
        public Class Parent { get; }
        public string Name { get; }
        public string FullName { get; }
        public ClassMemberType MemberType { get; }
        public SourcefilePosition SourceLocation { get; }
    }

    public abstract class AbstractClassMember : AbstractBytecode, IClassMember
    {
        private protected string _name;

        protected AbstractClassMember(SourcefilePosition sourceLocation, Class parent, string name, MemberModifier modifier)
        {
            Parent = parent;
            _name = name;
            Modifier = modifier;
            SourceLocation = sourceLocation;
        }

        public Class Parent { get; }

        public virtual string Name => _name;

        public virtual string FullName => Parent.FullName + '.' + Name;
        public MemberModifier Modifier { get; protected set; }
        public abstract ClassMemberType MemberType { get; }
        public SourcefilePosition SourceLocation { get; }

        public abstract Stack Evaluate(RuntimeBase vm, Stack stack);

        public override void Write(StringCache strings, Stream stream)
        {
            stream.Write(new[] { (byte)MemberType });
            stream.Write(BitConverter.GetBytes((int)Modifier));
            stream.Write(BitConverter.GetBytes(strings[Name]));
        }

        public override void Load(RuntimeBase vm, StringCache strings, byte[] data)
        {
            var index = 0;
            _Load(strings, data, ref index, out string name, out var modifier);
            _name = name;
            Modifier = modifier;
        }

        public static AbstractClassMember Read(RuntimeBase vm,
            StringCache strings, Class parent, byte[] data, ref int index)
        {
            var type = _Load(strings, data, ref index, out string name, out var modifier);
            AbstractClassMember member = type switch
            {
                ClassMemberType.Method => new Method(RuntimeBase.SystemSrcPos, parent, name, null, modifier), // todo fixme
                ClassMemberType.Property => new Property(RuntimeBase.SystemSrcPos, parent, name, null, modifier),
                _ => throw new ArgumentOutOfRangeException()
            };
            member.Load(vm, strings, data, ref index);
            return member;
        }

        private static ClassMemberType _Load(StringCache strings, byte[] data, ref int index, out string name,
            out MemberModifier modifier)
        {
            var type = (ClassMemberType)data[index];
            index += 1;
            modifier = (MemberModifier)BitConverter.ToInt32(data, index);
            index += 4;
            name = strings.Find(data, ref index);
            return type;
        }
    }
}