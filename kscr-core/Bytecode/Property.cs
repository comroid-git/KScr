using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using KScr.Core.Core;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;
using KScr.Core.Util;

namespace KScr.Core.Bytecode
{
    public sealed class Property : AbstractClassMember, IObjectRef
    {
        public bool Gettable;
        public ExecutableCode? Getter;
        public bool Settable;
        public ExecutableCode? Setter;
        public bool Inittable;
        public ExecutableCode? Initter;

        public Property(SourcefilePosition sourceLocation, Class parent, string name, ITypeInfo returnType, MemberModifier modifier) : base(sourceLocation, parent, name, modifier)
        {
            ReturnType = returnType;
        }

        public override string FullName => Parent.FullName + '.' + Name + ": " + ReturnType.FullName;

        public override ClassMemberType MemberType => ClassMemberType.Property;
        public ITypeInfo ReturnType { get; private set; }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => new[] { Getter, Setter }
            .Where(x => x != null).Cast<ExecutableCode>();

        public override Stack Evaluate(RuntimeBase vm, Stack stack) => ReadValue(vm, stack, stack.Alp!.Value);

        private string CreateSubKey(string ownerKey)
        {
            return $"property-{ownerKey}.{Name}";
        }

        public override void Write(StringCache strings, Stream stream)
        {
            base.Write(strings, stream);
            stream.Write(BitConverter.GetBytes(strings[ReturnType.FullDetailedName]));
            stream.Write(new[] {(byte) ((Gettable ? 0b0001 : 0)
                                        | (Settable ? 0b0010 : 0) 
                                        | (Getter != null ? 0b0100 : 0)
                                        | (Setter != null ? 0b1000 : 0))});
            Getter?.Write(strings, stream);
            Setter?.Write(strings, stream);
        }

        public override void Load(RuntimeBase vm, StringCache strings, byte[] data, ref int i)
        {
            base.Load(vm, strings, data, ref i);
            ReturnType = vm.FindType(strings.Find(data, ref i), owner: Parent)!;
            Gettable = (data[i] & 0b0001) != 0; 
            Settable = (data[i] & 0b0010) != 0; 
            var getter = (data[i] & 0b0100) != 0; 
            var setter = (data[i] & 0b1000) != 0; 
            i += 1;
            if (getter)
            {
                Getter = new ExecutableCode();
                Getter.Load(vm, strings, data, ref i);
            }
            if (setter)
            {
                Setter = new ExecutableCode();
                Setter.Load(vm, strings, data, ref i);
            }
        }

        public new static Property Read(RuntimeBase vm, StringCache strings, Class parent, byte[] data, ref int i)
        {
            return (AbstractClassMember.Read(vm, strings, parent, data, ref i) as Property)!;
        }

        public int Length => 1;
        public bool IsPipe => false; // todo
        public Stack ReadValue(RuntimeBase vm, Stack stack, IObject from)
        { // evaluate property with object
            if (Gettable && (ReadAccessor is not ExecutableCode ra || ra.Main.Count == 0))
            { // is auto-property
                if (vm[stack, VariableContext.Property, CreateKey(@from)] == null)
                    stack[StackOutput.Alp | StackOutput.Omg] = vm[stack, VariableContext.Property, CreateKey(@from)] 
                        = new ObjectRef(ReturnType.ResolveType(stack[StackOutput.Default]!.Value.Type));
                else stack[StackOutput.Alp | StackOutput.Omg] = vm[stack, VariableContext.Property, CreateKey(@from)];
                return stack;
            }
            if (ReadAccessor != null)
            {
                ReadAccessor.Evaluate(vm, stack.Output()).Copy(StackOutput.Alp, StackOutput.Alp | StackOutput.Omg);
                return stack;
            }
            throw new FatalException("Property " + FullName + " is not gettable"); // invalid state?
        }

        public Stack WriteValue(RuntimeBase vm, Stack stack, IObject to)
        { // evaluate property with object
            if (Settable && (WriteAccessor is not ExecutableCode wa || wa.Main.Count == 0))
            { // is auto-property
                vm[stack, VariableContext.Absolute, CreateKey(to)] = stack[StackOutput.Default];
                return stack;
            }
            if (WriteAccessor != null)
                return WriteAccessor.Evaluate(vm, stack);
            throw new InternalException("Property " + FullName + " is not settable");
        }

        private string CreateKey(IObject parent)
        {
            return $"property-{parent.GetKey()}.{Name}";
        }

        public IEvaluable? ReadAccessor
        {
            get => Getter;
            set => Getter = (value as ExecutableCode)!;
        }

        public IEvaluable? WriteAccessor
        {
            get => Setter;
            set => Setter = (value as ExecutableCode)!;
        }

        public IObject Value
        {
            get
            {
                Console.Error.WriteLine("Error: Property needs evaluation");
                return IObject.Null;
            }
            set => Console.Error.WriteLine("Error: Property needs evaluation");
        }

        public IClassInstance Type => ReturnType.ResolveType(Parent.DefaultInstance);

        public IObject this[RuntimeBase vm, Stack stack, int i]
        {
            get => Value;
            set => Value = value;
        }
    }
}