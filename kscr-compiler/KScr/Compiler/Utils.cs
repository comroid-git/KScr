using System;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Core.Bytecode;
using KScr.Core.Model;
using static KScr.Antlr.KScrLexer;
using static KScr.Core.Bytecode.MemberModifier;
using static KScr.Core.Model.ClassType;
using static KScr.Core.Model.Operator;

namespace KScr.Compiler;

public static class Utils
{
    public static SourcefilePosition ToSrcPos(this ParserRuleContext context) =>
        new()
        {
            SourcefilePath = context.Start.TokenSource.SourceName,
            SourcefileLine = context.Start.Line,
            SourcefileCursor = context.Start.Column
        };

    public static MemberModifier ToModifier(this KScrParser.ModifiersContext context)
    {
        MemberModifier mod = 0;
        foreach (var each in context.modifier())
            mod |= each.ToModifier();
        return mod;
    }

    public static MemberModifier ToModifier(this KScrParser.ModifierContext context) => context.Start.TokenIndex switch
    {
        PUBLIC => Public,
        INTERNAL => Internal,
        PROTECTED => Protected,
        PRIVATE => Private,
        STATIC => Static,
        FINAL => Final,
        ABSTRACT => Abstract,
        SYNCHRONIZED => Synchronized,
        NATIVE => Native,
        _ => throw new ArgumentOutOfRangeException(nameof(context.Start.TokenIndex), context.Start.TokenIndex, "Invalid Modifier")
    };

    public static ClassType ToClassType(this KScrParser.ClassTypeContext context) => context.Start.TokenIndex switch
    {
        CLASS => ClassType.Class,
        INTERFACE => Interface,
        ENUM => ClassType.Enum,
        ANNOTATION => Annotation,
        _ => throw new ArgumentOutOfRangeException(nameof(context.Start.TokenIndex), context.Start.TokenIndex, "Invalid Class Type")
    };

    public static Operator ToOperator(this ParserRuleContext context) => context.Start.TokenIndex switch
    {
        PLUS => Plus,
        MINUS => Minus,
        STAR => Multiply,
        SLASH => Divide,
        PERCENT => Modulus,
        BITAND => BitAnd,
        BITOR => BitOr,
        AND => LogicAnd,
        OR => LogicOr,
        EXCLAMATION => LogicNot,
        EQUAL => Operator.Equals,
        INEQUAL => NotEquals,
        GREATEREQ => GreaterEq,
        GREATER => Greater,
        LESSEREQ => LesserEq,
        LESSER => Lesser,
        LSHIFT => LShift,
        RSHIFT => RShift,
        ULSHIFT => ULShift,
        URSHIFT => URShift,
        QUESTION => NullFallback,
        PLUSPLUS => Increment,
        MINUSMINUS => Decrement,
        _ => throw new ArgumentOutOfRangeException(nameof(context.Start.TokenIndex), context.Start.TokenIndex, "Invalid Operator")
    };
}