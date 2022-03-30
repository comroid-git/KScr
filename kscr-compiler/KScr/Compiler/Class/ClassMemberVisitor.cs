using KScr.Antlr;
using KScr.Lib.Bytecode;

namespace KScr.Compiler.Class;

public abstract class ClassMemberVisitor<T> : KScrParserBaseVisitor<T> where T : IClassMember {}
public class MethodVisitor : ClassMemberVisitor<Method> {}
public class ConstructorVisitor : ClassMemberVisitor<Method> {}
public class InitializerVisitor : ClassMemberVisitor<Method> {}
public class PropertyVisitor : ClassMemberVisitor<Property> {}
// todo: ClassDeclVisitor needs to be here (class needs to implement IClassMember)
