using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;
using KScr.Core.Util;

namespace KScr.Core.Bytecode
{
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
            Modifier = modifier | MemberModifier.Native;
            Parameters = parameters;
            ReturnType = returnType;
        }

        public Stack Evaluate(RuntimeBase vm, Stack stack)
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
        public SourcefilePosition SourceLocation => RuntimeBase.SystemSrcPos;

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

        public Method(SourcefilePosition sourceLocation, Class parent, string name, ITypeInfo returnType, MemberModifier modifier) : base(sourceLocation, parent, name, modifier)
        {
            ReturnType = returnType;
        }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => new[] { Body };

        public List<MethodParameter> Parameters { get; } = new();
        public ITypeInfo ReturnType { get; private set; }

        public override string FullName =>
            base.FullName + '(' + string.Join(", ", Parameters.Select(it => it.Type)) + ')';

        public override ClassMemberType MemberType => ClassMemberType.Method;

        public override Stack Evaluate(RuntimeBase vm, Stack stack)
        {
            Body.Evaluate(vm, stack);
            if (stack.State != State.Return && Name != ConstructorName && ReturnType.Name != "void")
                throw new FatalException("Invalid state after method: " + stack.State);
            return stack;
        }

        public override void Write(StringCache strings, Stream stream)
        {
            base.Write(strings, stream);
            strings.Push(stream, ReturnType.FullDetailedName);
            stream.Write(BitConverter.GetBytes(Parameters.Count));
            foreach (var parameter in Parameters)
                parameter.Write(strings, stream);
            if (!this.IsAbstract() && !this.IsNative() && Parent.ClassType is not ClassType.Interface or ClassType.Annotation)
                Body.Write(strings, stream);
        }

        public override void Load(RuntimeBase vm, StringCache strings, byte[] data, ref int i)
        {
            ReturnType = vm.FindType(strings.Find(data, ref i), Parent.Package)!;
            int len = BitConverter.ToInt32(data, i);
            i += 4;
            Parameters.Clear();
            for (; len > 0; len--)
                Parameters.Add(MethodParameter.Read(vm, strings, data, ref i));
            if (!this.IsAbstract() && !this.IsNative() && Parent.ClassType is not ClassType.Interface or ClassType.Annotation)
            {
                Body = new ExecutableCode();
                Body.Load(vm, strings, data, ref i);
            }
        }

        public new static Method Read(RuntimeBase vm, StringCache strings, Class parent, byte[] data, ref int i)
        {
            return (AbstractClassMember.Read(vm, strings, parent, data, ref i) as Method)!;
        }
    }

    public class MethodParameter : IBytecode
    {
        public ITypeInfo Type { get; set; }
        public string Name { get; set; }

        public void Write(StringCache strings, Stream stream)
        {
            strings.Push(stream, Name);
            strings.Push(stream, Type.FullDetailedName);
        }

        public void Load(RuntimeBase vm, StringCache strings, byte[] data, ref int index)
        {
            Name = strings.Find(data, ref index);
            Type = vm.FindType(strings.Find(data, ref index))!;
        }

        public static MethodParameter Read(RuntimeBase vm, StringCache strings, byte[] data, ref int i)
        {
            var param = new MethodParameter();
            param.Load(vm, strings, data, ref i);
            return param;
        }
    }
}