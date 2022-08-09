using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;
using KScr.Core.Util;

namespace KScr.Bytecode.Adapter;

public class BytecodeAdapterV0_10 : AbstractBytecodeAdapter
{
    [Flags]
    private enum PropState : byte
    {
        Automatic = 0b000_000,
        Gettable  = 0b000_001,
        Settable  = 0b000_010,
        Initable  = 0b000_100,
        HasGetter = 0b001_000,
        HasSetter = 0b010_000,
        HasIniter = 0b100_000,
    }

    public BytecodeAdapterV0_10() : base(BytecodeVersion.V_0_10)
    {
    }

    public override void Write(StringCache strings, Stream stream, IBytecode bytecode)
    {
        if (bytecode == null)
            throw new NullReferenceException();

        void WriteElementType()
        {
            WriteByte((byte)bytecode.ElementType);
        }

        void WriteByte(byte b)
        {
            stream!.Write(new[] { b });
        }

        void WriteInt(int i)
        {
            stream!.Write(BitConverter.GetBytes(i));
        }

        void WriteUInt(uint ui)
        {
            stream!.Write(BitConverter.GetBytes(ui));
        }

        void WriteULong(ulong ul)
        {
            stream!.Write(BitConverter.GetBytes(ul));
        }

        void WriteString(string str)
        {
            WriteInt(strings[str]);
        }

        void WriteNewline()
        {
            stream!.Write(StringCache.NewLineBytes);
        }

        void WriteArray<T>(T[] arr) where T : IBytecode
        {
            stream.Write(BitConverter.GetBytes(arr.Length));
            foreach (var node in arr)
                Write(strings, stream, node);
        }

        WriteElementType();

        if (bytecode is Class cls)
        {
            WriteByte((byte)cls.MemberType);
            WriteByte((byte)cls.ClassType);
            WriteUInt((uint)cls.Modifier);
            WriteString(cls.Name);
            WriteArray(cls.Imports.Select(IBytecode.String).ToArray());
            WriteArray(cls._superclasses.Select(it => it.FullDetailedName).Select(IBytecode.String).ToArray());
            WriteArray(cls._interfaces.Select(it => it.FullDetailedName).Select(IBytecode.String).ToArray());
            WriteArray(cls.DeclaredMembers.Values.Where(x => x is not DummyMethod).ToArray());
        }
        else if (bytecode is Property prop)
        {
            WriteByte((byte)prop.MemberType);
            WriteUInt((uint)prop.Modifier);
            WriteString(prop.Name);
            WriteString(prop.ReturnType.FullDetailedName);
            Write(strings, stream, prop.SourceLocation);
            WriteByte((byte)
                ((prop.Gettable ? (byte)PropState.Gettable : 0) |
                 (prop.Settable ? (byte)PropState.Settable : 0) |
                 (prop.Inittable ? (byte)PropState.Initable : 0) |
                 (prop.Getter != null ? (byte)PropState.HasGetter : 0) |
                 (prop.Setter != null ? (byte)PropState.HasSetter : 0) |
                 (prop.Initter != null ? (byte)PropState.HasIniter : 0))
            );
            if (prop.Getter != null)
                Write(strings, stream, prop.Getter!);
            if (prop.Setter != null)
                Write(strings, stream, prop.Setter!);
            if (prop.Initter != null)
                Write(strings, stream, prop.Initter!);
        }
        else if (bytecode is Method mtd)
        {
            WriteByte((byte)mtd.MemberType);
            WriteUInt((uint)mtd.Modifier);
            WriteString(mtd.Name);
            WriteString(mtd.ReturnType.FullDetailedName);
            Write(strings, stream, mtd.SourceLocation);
            WriteArray(mtd.Parameters.ToArray());
            Write(strings, stream, mtd.Body);
        }
        else if (bytecode is MethodParameter param)
        {
            WriteString(param.Type.FullDetailedName);
            WriteString(param.Name);
        }
        else if (bytecode is SourcefilePosition srcPos)
        {
            WriteInt(srcPos.SourcefileLine);
            WriteInt(srcPos.SourcefileCursor);
        }
        else if (bytecode is ExecutableCode code)
        {
            WriteArray(code.Main.ToArray());
        }
        else if (bytecode is Statement stmt)
        {
            WriteUInt((uint)stmt.Type);
            WriteUInt((uint)stmt.CodeType);
            WriteString(stmt.Arg ?? string.Empty);
            WriteString(stmt.TargetType.FullDetailedName);
            WriteArray(stmt.Main.ToArray());
            WriteByte((byte)(stmt.CatchFinally != null ? 1 : 0));
            if (stmt.CatchFinally != null)
                Write(strings, stream, stmt.CatchFinally!);
        }
        else if (bytecode is StatementComponent comp)
        {
            WriteUInt((uint)comp.Type);
            WriteUInt((uint)comp.CodeType);
            WriteByte((byte)comp.VariableContext);
            WriteULong(comp.ByteArg);
            WriteString(comp.Arg ?? string.Empty);
            WriteArray(comp.Args.Select(IBytecode.String).ToArray());
            Write(strings, stream, comp.SourcefilePosition);
            var memberState = comp.GetComponentMember();
            WriteByte((byte)memberState);
            if ((memberState & ComponentMember.SubStatement) != 0)
                Write(strings, stream, comp.SubStatement!);
            if ((memberState & ComponentMember.AltStatement) != 0)
                Write(strings, stream, comp.AltStatement!);
            if ((memberState & ComponentMember.SubComponent) != 0)
                Write(strings, stream, comp.SubComponent!);
            if ((memberState & ComponentMember.AltComponent) != 0)
                Write(strings, stream, comp.AltComponent!);
            if ((memberState & ComponentMember.PostComponent) != 0)
                Write(strings, stream, comp.PostComponent!);
            if ((memberState & ComponentMember.InnerCode) != 0)
                Write(strings, stream, comp.InnerCode!);
        }
        else if (bytecode is LiteralBytecode<byte> litB)
        {
            WriteByte(litB.Value);
        }
        else if (bytecode is LiteralBytecode<int> litI)
        {
            WriteInt(litI.Value);
        }
        else if (bytecode is LiteralBytecode<uint> litUI)
        {
            WriteUInt(litUI.Value);
        }
        else if (bytecode is LiteralBytecode<ulong> litUL)
        {
            WriteULong(litUL.Value);
        }
        else if (bytecode is LiteralBytecode<string> litS)
        {
            WriteString(litS.Value);
        }
        else
        {
            throw new NotSupportedException("Bytecode not supported: " + bytecode);
        }

        stream.Flush();
    }

    public override T Load<T>(RuntimeBase vm, StringCache strings, Stream stream, Package pkg, Class? cls)
    {
        byte[] buf;

        BytecodeElementType ReadElementType()
        {
            return (BytecodeElementType)ReadByte();
        }

        byte ReadByte()
        {
            stream.Read(buf = new byte[1], 0, 1);
            return buf[0];
        }

        int ReadInt()
        {
            stream!.Read(buf = new byte[4], 0, 4);
            return BitConverter.ToInt32(buf);
        }

        uint ReadUInt()
        {
            stream!.Read(buf = new byte[4], 0, 4);
            return BitConverter.ToUInt32(buf);
        }

        ulong ReadULong()
        {
            stream!.Read(buf = new byte[8], 0, 8);
            return BitConverter.ToUInt64(buf);
        }

        string ReadString()
        {
            var id = ReadInt();
            return strings[id] ?? throw new NotSupportedException("Missing string with id " + id);
        }

        void ReadNewline()
        {
            stream!.Read(buf = new byte[StringCache.NewLineBytes.Length], 0, StringCache.NewLineBytes.Length);
        }

        T2[] ReadArray<T2>()
        {
            var l = ReadInt();
            var yields = new T2[l];
            for (var i = 0; i < l; i++)
                yields[i] = Load<T2>(vm, strings, stream, pkg, cls);
            return yields;
        }

        var type = ReadElementType();
        ClassMemberType memberType;
        MemberModifier mod;
        string name;
        IClassInstance? returnType;
        StatementComponentType sType;
        BytecodeType codeType;
        string? sArg;
        SourcefilePosition srcPos;
        switch (type)
        {
            case BytecodeElementType.Byte:
                return (T)(object)ReadByte();
            case BytecodeElementType.Int32:
                return (T)(object)ReadInt();
            case BytecodeElementType.UInt32:
                return (T)(object)ReadUInt();
            case BytecodeElementType.UInt64:
                return (T)(object)ReadULong();
            case BytecodeElementType.String:
                return (T)(object)ReadString();
            case BytecodeElementType.Class:
                memberType = (ClassMemberType)ReadByte();
                if (memberType != ClassMemberType.Class)
                    throw new FatalException(
                        $"Unable to load class {cls?.CanonicalName}; invalid member type at {stream.Position}");
                var classType = (ClassType)ReadByte();
                mod = (MemberModifier)ReadUInt();
                name = ReadString();
                cls = new Class(pkg, name, false, mod, classType);
                var imports = ReadArray<string>();
                var superclasses = ReadArray<string>().Select(x => vm.FindType(x));
                var interfaces = ReadArray<string>().Select(x => vm.FindType(x));
                var members = ReadArray<IClassMember>();
                foreach (var import in imports) cls.Imports.Add(import);
                foreach (var superclass in superclasses) cls._superclasses.Add(superclass);
                foreach (var iface in interfaces) cls._interfaces.Add(iface);
                foreach (var member in members) cls.DeclaredMembers[member.Name] = member;
                return (T)(object)cls;
            case BytecodeElementType.Property:
                memberType = (ClassMemberType)ReadByte();
                if (memberType != ClassMemberType.Property)
                    throw new FatalException(
                        $"Unable to load class {cls?.CanonicalName}; invalid member type at {stream.Position}");
                mod = (MemberModifier)ReadUInt();
                name = ReadString();
                returnType = vm.FindType(ReadString());
                srcPos = Load<SourcefilePosition>(vm, strings, stream, pkg, cls);
                var propState = (PropState)ReadByte();
                ExecutableCode? getter, setter, initter = setter = getter = null;
                if ((propState & PropState.HasGetter) != 0)
                    getter = Load<ExecutableCode>(vm, strings, stream, pkg, cls);
                else if ((propState & PropState.HasSetter) != 0)
                    setter = Load<ExecutableCode>(vm, strings, stream, pkg, cls);
                else if ((propState & PropState.HasIniter) != 0)
                    initter = Load<ExecutableCode>(vm, strings, stream, pkg, cls);
                return (T)(object)new Property(srcPos, cls!, name, returnType, mod)
                {
                    Gettable = (propState & PropState.Gettable) != 0,
                    Settable = (propState & PropState.Settable) != 0,
                    Inittable = (propState & PropState.Initable) != 0,
                    Getter = getter,
                    Setter = setter,
                    Initter = initter
                };
            case BytecodeElementType.Method:
                memberType = (ClassMemberType)ReadByte();
                if (memberType != ClassMemberType.Method)
                    throw new FatalException(
                        $"Unable to load class {cls?.CanonicalName}; invalid member type at {stream.Position}");
                mod = (MemberModifier)ReadUInt();
                name = ReadString();
                returnType = vm.FindType(ReadString());
                srcPos = Load<SourcefilePosition>(vm, strings, stream, pkg, cls);
                var parameters = ReadArray<MethodParameter>();
                var body = Load<ExecutableCode>(vm, strings, stream, pkg, cls);
                var mtd = new Method(srcPos, cls!, name, returnType, mod) { Body = body };
                mtd.Parameters.AddRange(parameters);
                return (T)(object)mtd;
            case BytecodeElementType.MethodParameter:
                returnType = vm.FindType(ReadString());
                name = ReadString();
                return (T)(object)new MethodParameter { Name = name, Type = returnType };
            case BytecodeElementType.SourcePosition:
                return (T)(object)new SourcefilePosition
                {
                    SourcefileLine = ReadInt(),
                    SourcefileCursor = ReadInt()
                };
            case BytecodeElementType.CodeBlock:
                var code = ReadArray<Statement>();
                var block = new ExecutableCode();
                block.Main.AddRange(code);
                return (T)(object)block;
            case BytecodeElementType.Statement:
                if (cls == null)
                    throw new FatalException("Containing class cannot be null");
                sType = (StatementComponentType)ReadUInt();
                codeType = (BytecodeType)ReadUInt();
                sArg = ReadString();
                var targetType = vm.FindType(ReadString());
                var comps = ReadArray<StatementComponent>();
                StatementComponent? finallyBlock = null;
                if (ReadByte() == 1) finallyBlock = Load<StatementComponent>(vm, strings, stream, pkg, cls);
                var stmt = new Statement
                {
                    Type = sType,
                    CodeType = codeType,
                    Arg = sArg,
                    TargetType = targetType,
                    CatchFinally = finallyBlock
                };
                stmt.Main.AddRange(comps);
                return (T)(object)stmt;
            case BytecodeElementType.Component:
                if (cls == null)
                    throw new FatalException("Containing class cannot be null");
                sType = (StatementComponentType)ReadUInt();
                codeType = (BytecodeType)ReadUInt();
                var varctx = (VariableContext)ReadByte();
                var byteArg = ReadULong();
                sArg = ReadString();
                var sArgs = ReadArray<string>();
                srcPos = Load<SourcefilePosition>(vm, strings, stream, pkg, cls);
                var memberState = (ComponentMember)ReadByte();
                Statement? subStmt, altStmt = subStmt = null;
                StatementComponent? subComp, altComp, postComp = altComp = subComp = null;
                ExecutableCode? innerCode = null;
                if ((memberState & ComponentMember.SubStatement) != 0)
                    subStmt = Load<Statement>(vm, strings, stream, pkg, cls);
                if ((memberState & ComponentMember.AltStatement) != 0)
                    altStmt = Load<Statement>(vm, strings, stream, pkg, cls);
                if ((memberState & ComponentMember.SubComponent) != 0)
                    subComp = Load<StatementComponent>(vm, strings, stream, pkg, cls);
                if ((memberState & ComponentMember.AltComponent) != 0)
                    altComp = Load<StatementComponent>(vm, strings, stream, pkg, cls);
                if ((memberState & ComponentMember.PostComponent) != 0)
                    postComp = Load<StatementComponent>(vm, strings, stream, pkg, cls);
                if ((memberState & ComponentMember.InnerCode) != 0)
                    innerCode = Load<ExecutableCode>(vm, strings, stream, pkg, cls);
                return (T)(object)new StatementComponent
                {
                    Type = sType,
                    CodeType = codeType,
                    VariableContext = varctx,
                    ByteArg = byteArg,
                    Arg = sArg,
                    Args = sArgs.ToList(),
                    SubStatement = subStmt,
                    AltStatement = altStmt,
                    SubComponent = subComp,
                    AltComponent = altComp,
                    PostComponent = postComp,
                    InnerCode = innerCode,
                    SourcefilePosition = srcPos
                };
        }

        throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid ElementType");
    }
}