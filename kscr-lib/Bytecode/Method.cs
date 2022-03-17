using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public class MethodParameter : IBytecode
    {
        public IClassInstance Type { get; set; }
        public string Name { get; set; }

        public void Write(Stream stream)
        {
            byte[] buf = RuntimeBase.Encoding.GetBytes(Name);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
            buf = RuntimeBase.Encoding.GetBytes(Type.FullName);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
        }

        public void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            var len = BitConverter.ToInt32(data, index);
            index += 4;
            Name = RuntimeBase.Encoding.GetString(data, index, len);
            index += len;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            Type = vm.FindType(RuntimeBase.Encoding.GetString(data, index, len))!;
            index += len;
        }

        public static MethodParameter Read(RuntimeBase vm, byte[] data, ref int i)
        {
            var param = new MethodParameter();
            param.Load(vm, data, ref i);
            return param;
        }
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

        public DummyMethod(Class parent, string name, MemberModifier modifier, ITypeInfo returnType,
            List<MethodParameter> parameters)
        {
            Parent = parent;
            Name = name;
            Modifier = modifier;
            Parameters = parameters;
            ReturnType = returnType;
        }

        public void Evaluate(RuntimeBase vm, Stack stack, StackOutput copyFromStack = StackOutput.None)
        {
            throw new InvalidOperationException("Cannot evaluate a dummy method. This is an invalid state.");
        }

        public Class Parent { get; set; }
        public string Name { get; set; }
        public string FullName => Parent.FullName + '.' + Name + ": " + ReturnType.FullName;
        public MemberModifier Modifier { get; set; }
        public List<MethodParameter> Parameters { get; set; }
        public ITypeInfo ReturnType { get; set; }
        public ClassMemberType MemberType => ClassMemberType.Method;

        public IEvaluable? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev)
        {
            throw new InvalidOperationException("Cannot evaluate a dummy method. This is an invalid state.");
        }
    }

    public sealed class Method : AbstractClassMember, IMethod
    {
        public const string ConstructorName = "ctor";
        public const string StaticInitializerName = "cctor";
        public ExecutableCode Body = null!;

        public Method(Class parent, string name, ITypeInfo returnType, MemberModifier modifier) : base(parent, name,
            modifier)
        {
            ReturnType = returnType;
        }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => new[] { Body };

        public List<MethodParameter> Parameters { get; } = new();
        public ITypeInfo ReturnType { get; private set; }

        public override string FullName =>
            base.FullName + '(' + string.Join(", ", Parameters.Select(it => it.Type)) + ')';

        public override ClassMemberType MemberType => ClassMemberType.Method;

        public override void Evaluate(RuntimeBase vm, Stack stack, StackOutput copyFromStack = StackOutput.None)
        {
            Body.Evaluate(vm, stack);
            if (stack.State != State.Return && Name != ConstructorName && ReturnType.Name != "void")
                throw new FatalException("Invalid state after method: " + stack.State);
            stack.CopyFromStack(copyFromStack);
        }

        public override void Write(Stream stream)
        {
            base.Write(stream);
            byte[] buf = RuntimeBase.Encoding.GetBytes(ReturnType.FullName);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
            stream.Write(BitConverter.GetBytes(Parameters.Count));
            foreach (var parameter in Parameters)
                parameter.Write(stream);
            if (!this.IsAbstract())
                Body.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int i)
        {
            var len = BitConverter.ToInt32(data, i);
            i += 4;
            ReturnType = vm.FindType(RuntimeBase.Encoding.GetString(data, i, len), Parent.Parent)!;
            i += len;
            len = BitConverter.ToInt32(data, i);
            i += 4;
            Parameters.Clear();
            for (; len > 0; len--)
                Parameters.Add(MethodParameter.Read(vm, data, ref i));
            if (!this.IsAbstract())
            {
                Body = new ExecutableCode();
                Body.Load(vm, data, ref i);
            }
        }

        public new static Method Read(RuntimeBase vm, Class parent, byte[] data, ref int i)
        {
            return (AbstractClassMember.Read(vm, parent, data, ref i) as Method)!;
        }
    }
}