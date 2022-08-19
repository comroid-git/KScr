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

    public override void Write(Stream stream, StringCache strings, Class cls)
    {
        Write(stream, (byte)cls.MemberType);
        Write(stream, (byte)cls.ClassType);
        Write(stream, (uint)cls.Modifier);
        Write(stream, strings, cls.Name);
        Write(stream, strings, cls.Imports.Select(IBytecode.String).ToArray());
        Write(stream, strings, cls._superclasses.Select(it => it.FullDetailedName).Select(IBytecode.String).ToArray());
        Write(stream, strings, cls._interfaces.Select(it => it.FullDetailedName).Select(IBytecode.String).ToArray());
        Write(stream, strings, cls.DeclaredMembers.Values.Where(x => x is not DummyMethod).ToArray());
    }

    public override void Write(Stream stream, StringCache strings, Property prop)
    {
        Write(stream, (byte)prop.MemberType);
        Write(stream, (uint)prop.Modifier);
        Write(stream, strings, prop.Name);
        Write(stream, strings, prop.ReturnType.FullDetailedName);
        Write(stream, strings, prop.SourceLocation);
        Write(stream, (byte)
            ((prop.Gettable ? 0b000_001 : 0) |
             (prop.Settable ? 0b000_010 : 0) |
             (prop.Inittable ? 0b000_100 : 0) |
             (prop.Getter != null ? 0b001_000 : 0) |
             (prop.Setter != null ? 0b010_000 : 0) |
             (prop.Initter != null ? 0b100_000 : 0)));
        if (prop.Getter != null)
            Write(stream, strings, prop.Getter!);
        if (prop.Setter != null)
            Write(stream, strings, prop.Setter!);
        if (prop.Initter != null)
            Write(stream, strings, prop.Initter!);
    }

    public override void Write(Stream stream, StringCache strings, Method mtd)
    {
        Write(stream, (byte)mtd.MemberType);
        Write(stream, (uint)mtd.Modifier);
        Write(stream, strings, mtd.Name);
        Write(stream, strings, mtd.ReturnType.FullDetailedName);
        Write(stream, strings, mtd.SourceLocation);
        Write(stream, strings, mtd.Parameters.ToArray());
        Write(stream, strings, mtd.Body);
    }

    public override void Write(Stream stream, StringCache strings, MethodParameter param)
    {
        Write(stream, strings, param.Type.FullDetailedName);
        Write(stream, strings, param.Name);
    }

    public override void Write(Stream stream, StringCache strings, SourcefilePosition srcPos)
    {
        Write(stream, srcPos.SourcefileLine);
        Write(stream, srcPos.SourcefileCursor);
    }

    public override void Write(Stream stream, StringCache strings, ExecutableCode code)
    {
        Write(stream, strings, code.Main.ToArray());
    }

    public override void Write(Stream stream, StringCache strings, Statement stmt)
    {
        Write(stream, (uint)stmt.Type);
        Write(stream, (uint)stmt.CodeType);
        Write(stream, strings, stmt.Arg ?? string.Empty);
        Write(stream, strings, stmt.TargetType.FullDetailedName);
        Write(stream, strings, stmt.Main.ToArray());
        Write(stream, (byte)(stmt.CatchFinally != null ? 1 : 0));
        if (stmt.CatchFinally != null)
            Write(stream, strings, stmt.CatchFinally!);
    }

    public override void Write(Stream stream, StringCache strings, StatementComponent comp)
    {
        Write(stream, (uint)comp.Type);
        Write(stream, (uint)comp.CodeType);
        Write(stream, (byte)comp.VariableContext);
        Write(stream, comp.ByteArg);
        Write(stream, strings, comp.Arg ?? string.Empty);
        Write(stream, strings, comp.Args.Select(IBytecode.String).ToArray());
        Write(stream, strings, comp.SourcefilePosition);
        var memberState = comp.GetComponentMember();
        Write(stream, (byte)memberState);
        if ((memberState & ComponentMember.SubStatement) != 0)
            Write(stream, strings, comp.SubStatement!);
        if ((memberState & ComponentMember.AltStatement) != 0)
            Write(stream, strings, comp.AltStatement!);
        if ((memberState & ComponentMember.SubComponent) != 0)
            Write(stream, strings, comp.SubComponent!);
        if ((memberState & ComponentMember.AltComponent) != 0)
            Write(stream, strings, comp.AltComponent!);
        if ((memberState & ComponentMember.PostComponent) != 0)
            Write(stream, strings, comp.PostComponent!);
        if ((memberState & ComponentMember.InnerCode) != 0)
            Write(stream, strings, comp.InnerCode!);
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