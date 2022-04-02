using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;

namespace KScr.Compiler.Class;

public abstract class ClassMemberVisitor<T> : AbstractVisitor<T> where T : IClassMember
{
    protected ClassMemberVisitor(RuntimeBase vm, KScrParser parser, CompilerContext ctx) : base(vm, parser, ctx)
    {
    }
}
public class MethodVisitor : ClassMemberVisitor<Method>
{
    public MethodVisitor(RuntimeBase vm, KScrParser parser, CompilerContext ctx) : base(vm, parser, ctx)
    {
    }
}
public class ConstructorVisitor : ClassMemberVisitor<Method>
{
    public ConstructorVisitor(RuntimeBase vm, KScrParser parser, CompilerContext ctx) : base(vm, parser, ctx)
    {
    }
}
public class InitializerVisitor : ClassMemberVisitor<Method>
{
    public InitializerVisitor(RuntimeBase vm, KScrParser parser, CompilerContext ctx) : base(vm, parser, ctx)
    {
    }
}
public class PropertyVisitor : ClassMemberVisitor<Property>
{
    public PropertyVisitor(RuntimeBase vm, KScrParser parser, CompilerContext ctx) : base(vm, parser, ctx)
    {
    }
}
