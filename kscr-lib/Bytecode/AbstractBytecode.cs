using System.Collections.Generic;
using System.IO;

namespace KScr.Lib.Bytecode
{
    public enum BytecodeMemberType
    {
        Class,
        ClassMember,
        Code
    }
    
    public abstract class AbstractBytecode
    {
        protected abstract IEnumerable<AbstractBytecode> BytecodeMembers { get; }
        protected abstract BytecodeMemberType Type { get; }
        protected abstract byte[] ToBytes();
        protected abstract void ReadBytes(byte[] data);

        public void Read(string path)
        {
        }

        public void Write(string path)
        {
            foreach (var member in BytecodeMembers)
            {
                
            }
        }

        protected void Write(StreamWriter stream)
        {
            byte[] body = ToBytes();
            int len = body.Length;
            
            stream.Write(Type);
            stream.Write(len);
            stream.Write(body);
        }
    }
}