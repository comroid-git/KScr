using System.Reflection;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.System;
using KScr.Core.Store;
using KScr.Core.Util;

namespace KScr.Bytecode.Adapter;

public class BytecodeAdapterV0_10 : AbstractBytecodeAdapter
{
    public BytecodeAdapterV0_10() : base(BytecodeVersion.V_0_10)
    {
    }

    [Flags]
    private enum PropState : byte
    {
        Automatic = 0b000_000,
        Gettable = 0b000_001,
        Settable = 0b000_010,
        Initable = 0b000_100,
        HasGetter = 0b001_000,
        HasSetter = 0b010_000,
        HasIniter = 0b100_000
    }

    #region Class

    protected override void WriteClass(Stream stream, StringCache strings, Class cls)
    {
        Write(stream, (byte)cls.MemberType);
        Write(stream, (byte)cls.ClassType);
        Write(stream, (uint)cls.Modifier);
        Write(stream, strings, cls.Name);
        Write(stream, strings, cls.Imports.Select(IBytecode.String).ToArray());
        Write(stream, strings,
            cls.DeclaredSuperclasses.Select(it => it.FullDetailedName).Select(IBytecode.String).ToArray());
        Write(stream, strings,
            cls.DeclaredInterfaces.Select(it => it.FullDetailedName).Select(IBytecode.String).ToArray());
        Write(stream, strings, cls.DeclaredMembers.Values.Where(x => x is not DummyMethod).ToArray());
        Write(stream, strings, cls.TypeParameters.ToArray());
    }

    protected override Class ReadClass(RuntimeBase vm, Stream stream, StringCache strings, Package pkg)
    {
        ClassMemberType memberType;
        MemberModifier mod;
        string name;
        memberType = (ClassMemberType)ReadByte(stream);
        var classType = (ClassType)ReadByte(stream);
        mod = (MemberModifier)ReadUInt(stream);
        name = ReadString(stream, strings);
        if (memberType != ClassMemberType.Class)
            throw new FatalException(
                $"Unable to load class {name} invalid member type at {stream.Position}");
        var cls = new Class(pkg, name, false, mod, classType);
        var imports = ReadArray<string>(vm, stream, strings, pkg, cls);
        var superclasses = ReadArray<string>(vm, stream, strings, pkg, cls).Select(x => vm.FindType(x));
        var interfaces = ReadArray<string>(vm, stream, strings, pkg, cls).Select(x => vm.FindType(x));
        var members = ReadArray<IClassMember>(vm, stream, strings, pkg, cls);
        var tParams = ReadArray<TypeParameter>(vm, stream, strings, pkg, cls);
        foreach (var import in imports) cls.Imports.Add(import);
        foreach (var superclass in superclasses) cls.DeclaredSuperclasses.Add(superclass);
        foreach (var iface in interfaces) cls.DeclaredInterfaces.Add(iface);
        foreach (var member in members) cls.DeclaredMembers[member.Name] = member;
        foreach (var tParam in tParams) cls.TypeParameters.Add(tParam);
        cls.Initialize(vm);
        return cls;
    }

    #endregion

    #region Property

    protected override void WriteProperty(Stream stream, StringCache strings, Property prop)
    {
        Write(stream, (byte)prop.MemberType);
        Write(stream, (uint)prop.Modifier);
        Write(stream, strings, prop.Name);
        Write(stream, strings, prop.ReturnType.FullDetailedName);
        Write(stream, strings, prop.SourceLocation);
        var propState = (prop.Gettable ? PropState.Gettable : 0) |
                        (prop.Settable ? PropState.Settable : 0) |
                        (prop.Inittable ? PropState.Initable : 0) |
                        (prop.Getter != null && prop.Getter.Main.Count > 0 ? PropState.HasGetter : 0) |
                        (prop.Setter != null && prop.Setter.Main.Count > 0 ? PropState.HasSetter : 0) |
                        (prop.Initter != null && prop.Initter.Main.Count > 0 ? PropState.HasIniter : 0);
        Write(stream, (byte)propState);
        if ((propState & PropState.HasGetter) != 0)
            Write(stream, strings, prop.Getter!);
        if ((propState & PropState.HasSetter) != 0)
            Write(stream, strings, prop.Setter!);
        if ((propState & PropState.HasIniter) != 0)
            Write(stream, strings, prop.Initter!);
    }

    protected override Property ReadProperty(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class cls)
    {
        ClassMemberType memberType;
        MemberModifier mod;
        string name;
        IClassInstance? returnType;
        SourcefilePosition srcPos;
        memberType = (ClassMemberType)ReadByte(stream);
        if (memberType != ClassMemberType.Property)
            throw new FatalException(
                $"Unable to load class {cls?.CanonicalName}; invalid member type at {stream.Position}");
        mod = (MemberModifier)ReadUInt(stream);
        name = ReadString(stream, strings);
        returnType = vm.FindType(ReadString(stream, strings));
        srcPos = Load<SourcefilePosition>(vm, strings, stream, pkg, cls);
        var propState = (PropState)ReadByte(stream);
        ExecutableCode? getter, setter, initter = setter = getter = null;
        if ((propState & PropState.HasGetter) != 0)
            getter = Load<ExecutableCode>(vm, strings, stream, pkg, cls);
        else if ((propState & PropState.HasSetter) != 0)
            setter = Load<ExecutableCode>(vm, strings, stream, pkg, cls);
        else if ((propState & PropState.HasIniter) != 0)
            initter = Load<ExecutableCode>(vm, strings, stream, pkg, cls);
        return new Property(srcPos, cls!, name, returnType, mod)
        {
            Gettable = (propState & PropState.Gettable) != 0,
            Settable = (propState & PropState.Settable) != 0,
            Inittable = (propState & PropState.Initable) != 0,
            Getter = getter,
            Setter = setter,
            Initter = initter
        };
    }

    #endregion

    #region Method

    protected override void WriteMethod(Stream stream, StringCache strings, Method mtd)
    {
        Write(stream, (byte)mtd.MemberType);
        Write(stream, (uint)mtd.Modifier);
        Write(stream, strings, mtd.Name);
        Write(stream, strings, mtd.ReturnType.FullDetailedName);
        Write(stream, strings, mtd.SourceLocation);
        Write(stream, strings, mtd.Parameters.ToArray());
        if (mtd.Name == Method.ConstructorName)
            Write(stream, strings, mtd.SuperCalls.ToArray());
        Write(stream, strings, mtd.Body);
    }

    protected override Method ReadMethod(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class cls)
    {
        ClassMemberType memberType;
        MemberModifier mod;
        string name;
        IClassInstance? returnType;
        SourcefilePosition srcPos;
        memberType = (ClassMemberType)ReadByte(stream);
        if (memberType != ClassMemberType.Method)
            throw new FatalException(
                $"Unable to load class {cls?.CanonicalName}; invalid member type at {stream.Position}");
        mod = (MemberModifier)ReadUInt(stream);
        name = ReadString(stream, strings);
        returnType = vm.FindType(ReadString(stream, strings));
        srcPos = Load<SourcefilePosition>(vm, strings, stream, pkg, cls);
        var parameters = ReadArray<MethodParameter>(vm, stream, strings, pkg, cls);
        var supers = name == Method.ConstructorName
            ? ReadArray<StatementComponent>(vm, stream, strings, pkg, cls)
            : null;
        var body = Load<ExecutableCode>(vm, strings, stream, pkg, cls);
        var mtd = new Method(srcPos, cls!, name, returnType, mod) { Body = body };
        mtd.Parameters.AddRange(parameters);
        if (supers != null)
            mtd.SuperCalls.AddRange(supers);
        return mtd;
    }

    #endregion

    #region Method Parameter

    protected override void WriteMethodParameter(Stream stream, StringCache strings, MethodParameter param)
    {
        Write(stream, strings, param.Type.FullDetailedName);
        Write(stream, strings, param.Name);
    }

    protected override MethodParameter ReadMethodParameter(RuntimeBase vm, Stream stream, StringCache strings,
        Package pkg, Class cls)
    {
        string name;
        IClassInstance? returnType;
        returnType = vm.FindType(ReadString(stream, strings));
        name = ReadString(stream, strings);
        return new MethodParameter { Name = name, Type = returnType };
    }

    #endregion

    #region SrcPos

    protected override void WriteSrcPos(Stream stream, StringCache strings, SourcefilePosition srcPos)
    {
        Write(stream, srcPos.SourcefileLine);
        Write(stream, srcPos.SourcefileCursor);
    }

    protected override SourcefilePosition ReadSrcPos(RuntimeBase vm, Stream stream, StringCache strings, Package pkg,
        Class cls)
    {
        return new SourcefilePosition
        {
            SourcefileLine = ReadInt(stream),
            SourcefileCursor = ReadInt(stream)
        };
    }

    #endregion

    #region Code

    protected override void WriteCode(Stream stream, StringCache strings, ExecutableCode code)
    {
        Write(stream, strings, code.Main.ToArray());
    }

    protected override ExecutableCode ReadCode(RuntimeBase vm, Stream stream, StringCache strings, Package pkg,
        Class cls)
    {
        var code = ReadArray<Statement>(vm, stream, strings, pkg, cls);
        var block = new ExecutableCode();
        block.Main.AddRange(code);
        return block;
    }

    #endregion

    #region Statement

    protected override void WriteStatement(Stream stream, StringCache strings, Statement stmt)
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

    protected override Statement ReadStatement(RuntimeBase vm, Stream stream, StringCache strings, Package pkg,
        Class cls)
    {
        StatementComponentType sType;
        BytecodeType codeType;
        string? sArg;
        sType = (StatementComponentType)ReadUInt(stream);
        codeType = (BytecodeType)ReadUInt(stream);
        sArg = ReadString(stream, strings);
        var targetType = vm.FindType(ReadString(stream, strings));
        var comps = ReadArray<StatementComponent>(vm, stream, strings, pkg, cls);
        StatementComponent? finallyBlock = null;
        if (ReadByte(stream) == 1) finallyBlock = Load<StatementComponent>(vm, strings, stream, pkg, cls);
        var stmt = new Statement
        {
            Type = sType,
            CodeType = codeType,
            Arg = sArg,
            TargetType = targetType,
            CatchFinally = finallyBlock
        };
        stmt.Main.AddRange(comps);
        return stmt;
    }

    #endregion

    #region Component

    protected override void WriteComponent(Stream stream, StringCache strings, StatementComponent comp)
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

    protected override StatementComponent ReadComponent(RuntimeBase vm, Stream stream, StringCache strings, Package pkg,
        Class cls)
    {
        StatementComponentType sType;
        BytecodeType codeType;
        string? sArg;
        SourcefilePosition srcPos;
        sType = (StatementComponentType)ReadUInt(stream);
        codeType = (BytecodeType)ReadUInt(stream);
        var varctx = (VariableContext)ReadByte(stream);
        var byteArg = ReadULong(stream);
        sArg = ReadString(stream, strings);
        var sArgs = ReadArray<string>(vm, stream, strings, pkg, cls);
        srcPos = Load<SourcefilePosition>(vm, strings, stream, pkg, cls);
        var memberState = (ComponentMember)ReadByte(stream);
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
        return new StatementComponent
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

    #endregion

    #region Type Parameter
    
    protected override void WriteTypeParameters(Stream stream, StringCache strings, TypeParameter param)
    {
        Write(stream, strings, param.Name);
        Write(stream, (byte)param.Specialization);
        Write(stream, strings, param.SpecializationTarget.FullDetailedName);
    }

    protected override TypeParameter ReadTypeParameter(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class cls)
    {
        var name = ReadString(stream, strings);
        var spec = (TypeParameterSpecializationType)ReadByte(stream);
        var target = vm.FindType(ReadString(stream, strings), pkg, cls);
        return new TypeParameter(name, spec, target);
    }

    #endregion
}