using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public class MethodParameter
    {
        public IClassInstance Type { get; set; }
        public string Name { get; set; }
    }

    public interface IMethod : IClassMember
    {
        List<MethodParameter> Parameters { get; }
        ITypeInfo ReturnType { get; }
    }
    
    public sealed class DummyMethod : IMethod
    {
        public DummyMethod(Class parent, string name, MemberModifier modifier, ITypeInfo returnType) 
            : this(parent, name, modifier, returnType, new List<MethodParameter>())
        {
        }

        public DummyMethod(Class parent, string name, MemberModifier modifier, ITypeInfo returnType, List<MethodParameter> parameters)
        {
            Parent = parent;
            Name = name;
            Modifier = modifier;
            Parameters = parameters;
            ReturnType = returnType;
        }

        public IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev) 
            => throw new InvalidOperationException("Cannot evaluate a dummy method. This is an invalid state.");
        public IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0)
            => throw new InvalidOperationException("Cannot evaluate a dummy method. This is an invalid state.");

        public Class Parent { get; set; }
        public string Name { get; set; }
        public string FullName => Parent.FullName + '.' + Name;
        public MemberModifier Modifier { get; set; }
        public List<MethodParameter> Parameters { get; set; }
        public ITypeInfo ReturnType { get; set; }
        public ClassMemberType Type => ClassMemberType.Method;
    }

    public sealed class Method : AbstractClassMember, IMethod
    {
        public const string ConstructorName = "ctor";
        public ExecutableCode Body = null!;

        public Method(Class parent, string name, ITypeInfo returnType, MemberModifier modifier) : base(parent, name, modifier)
        {
            ReturnType = returnType;
        }

        public List<MethodParameter> Parameters { get; } = new();
        public ITypeInfo ReturnType { get; private set; }

        public override string FullName => base.FullName + '(' + string.Join(", ", Parameters.Select(it => it.Type)) + ')';

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => new[] { Body };
        public override ClassMemberType Type => ClassMemberType.Method;

        public override IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0)
        {
            state = Body.Evaluate(vm, ref rev);
            //if (state != State.Return)
//                throw new InternalException("Invalid state after method: " + state);
            state = State.Normal;
            return null;
        }

        public override void Write(Stream stream)
        {
            base.Write(stream);
            byte[] buf = RuntimeBase.Encoding.GetBytes(ReturnType.FullName);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
            Body.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int i)
        {
            int len = BitConverter.ToInt32(data, i);
            i += 4;
            ReturnType = vm.FindType(RuntimeBase.Encoding.GetString(data, i, len))!;
            i += len;
            Body = new ExecutableCode();
            Body.Load(vm, data, ref i);
        }

        public new static Method Read(RuntimeBase vm, Class parent, byte[] data, ref int i)
        {
            return (AbstractClassMember.Read(vm, parent, data, ref i) as Method)!;
        }
    }
}