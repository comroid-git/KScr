using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public sealed class Class : AbstractPackageMember, IClassRef, IRuntimeSite
    {
        public const string StaticInitializer = "initializer_static";
        
        public Class(Package parent, string name, MemberModifier? modifier = null) : base(parent, name, modifier ?? MemberModifier.Public)
        {
        }

        public IDictionary<string, IClassMember> DeclaredMembers { get; } =
            new ConcurrentDictionary<string, IClassMember>();

        public long TypeId { get; }

        public IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0) =>
            DeclaredMembers[StaticInitializer].Evaluate(vm, ref state, ref rev, alt);

        public void Write(FileInfo file) => Write(file.OpenWrite());
        
        public override void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(Name.Length));
            stream.Write(RuntimeBase.Encoding.GetBytes(Name));
            stream.Write(BitConverter.GetBytes((uint) Modifier));
            
            stream.Write(BitConverter.GetBytes(DeclaredMembers.Count));
            foreach (var member in BytecodeMembers)
            {
                member.Write(stream);
                stream.Write(NewLineBytes);
            }
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            DeclaredMembers.Clear();
            
            int len;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            Name = RuntimeBase.Encoding.GetString(data, index, len);
            index += len;
            Modifier = (MemberModifier) BitConverter.ToInt32(data, index);
            index += 4;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            for (int i = 0; i < len; i++)
            {
                AbstractClassMember member = AbstractClassMember.Read(vm, this, data, ref index);
                DeclaredMembers[member.Name] = member;
                index += NewLineBytes.Length;
            }
        }

        public static Class Read(RuntimeBase vm, FileInfo file, Package package)
        {
            var cls = new Class(package, file.Name);
            cls.Load(vm, File.ReadAllBytes(file.FullName));
            return cls;
        }
    }
}