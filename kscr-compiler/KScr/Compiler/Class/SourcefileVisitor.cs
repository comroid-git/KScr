using System.Collections.Generic;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler.Class;

public class SourcefileVisitor : AbstractVisitor<CompilerContext>
{
    public SourcefileVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override CompilerContext VisitFile(KScrParser.FileContext context)
    {
        var pkgName = context.packageDecl().id().GetText();
        var pkg = Package.RootPackage.GetOrCreatePackage(pkgName);
        var imports = new List<string>();
        foreach (var import in context.imports().importDecl()) 
            imports.Add(import.id().GetText());
        var subctx = new CompilerContext() { Package = pkg, Imports = imports };
        foreach (var classDecl in context.classDecl())
        {
            var info = VisitClassInfo(classDecl);
            var clsctx = new CompilerContext() { Parent = subctx, Class = info };
            new ClassVisitor(vm, clsctx).Visit(classDecl);
        }
        return subctx;
    }
}

public class ClassInfoVisitor : AbstractVisitor<ClassInfo>
{
    public ClassInfoVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override ClassInfo VisitClassDecl(KScrParser.ClassDeclContext context)
    {
        var pkgName = ctx.Package.FullName;
        var modifier = new ModifierVisitor().Visit(context.modifiers());
        var type = new ClassTypeVisitor().Visit(context.classType());
        var name = context.idPart().GetText();
        return new ClassInfo(modifier, type, name)
        {
            CanonicalName = $"{pkgName}.{name}",
            FullName = $"{pkgName}.{name}"
        };
    }
}
