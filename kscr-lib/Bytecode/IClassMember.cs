using System;
using System.Collections.Generic;
using System.IO;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public enum ClassMemberType : byte
    {
        Method,
        Field
    }
    
    public interface IClassMember : IRuntimeSite, IModifierContainer
    {
        public Class Parent { get; }
        public string Name { get; }
        public string FullName { get; }
        public ClassMemberType Type { get; } 
    }

    public abstract class AbstractClassMember : AbstractBytecode, IClassMember
    {
        protected AbstractClassMember(Class parent, string name, MemberModifier modifier)
        {
            Parent = parent;
            Name = name;
            Modifier = modifier;
        }

        public Class Parent { get; }
        public string Name { get; protected set; }
        public MemberModifier Modifier { get; protected set; }
        public string FullName => Parent.FullName + '.' + Name;
        public abstract ClassMemberType Type { get; }

        public abstract IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0);

        public override void Write(Stream stream)
        {
            stream.Write(new []{(byte)Type});
            stream.Write(BitConverter.GetBytes(Name.Length));
            stream.Write(RuntimeBase.Encoding.GetBytes(Name));
            stream.Write(BitConverter.GetBytes((int)Modifier));
        }

        public override void Load(RuntimeBase vm, byte[] data)
        {
            int index = 0;
            _Load(data, ref index, out string name, out var modifier);
            Name = name;
            Modifier = modifier;
        }

        public static AbstractClassMember Read(RuntimeBase vm, Class parent, byte[] data, ref int index)
        {
            ClassMemberType type = _Load(data, ref index, out string name, out var modifier);
            AbstractClassMember member = type switch
            {
                ClassMemberType.Method => new Method(parent, name, modifier),
                ClassMemberType.Field => new Field(parent, name, modifier),
                _ => throw new ArgumentOutOfRangeException()
            };
            member.Load(vm, data, ref index);
            return member;
        }

        private static ClassMemberType _Load(byte[] data, ref int index, out string name, out MemberModifier modifier)
        {
            int len;
            var type = data[index];
            index += 1;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            name = RuntimeBase.Encoding.GetString(data, index, len);
            index += len;
            modifier = (MemberModifier)BitConverter.ToInt32(data, index);
            index += 4;
            return (ClassMemberType)type;
        }
    }
}