using System.Linq;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler.Class;

public class ClassVisitor : AbstractVisitor<Core.Std.Class>
{
    public ClassVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    private Core.Std.Class cls => ctx.Class!.AsClass(vm);

    public override Core.Std.Class VisitClassDecl(KScrParser.ClassDeclContext context)
    {
        if (context.genericTypeDefs() is { } defs)
            foreach (var genTypeDef in defs.genericTypeDef())
                if (cls.TypeParameters.All(x => x.Name != genTypeDef.idPart().GetText()))
                    cls.TypeParameters.Add(VisitTypeParameter(genTypeDef));
        if (context.objectExtends() is { } ext)
            foreach (var extendsType in ext.type())
                cls._superclasses.Add(VisitTypeInfo(extendsType).AsClassInstance(vm));
        if (context.objectImplements() is { } impl)
            foreach (var implementsType in impl.type())
                cls._interfaces.Add(VisitTypeInfo(implementsType).AsClassInstance(vm));

        foreach (var each in context.member())
        {
            var member = VisitClassMember(each);
            cls.DeclaredMembers[member.Name] = member;
        }

        return cls;
    }
}
