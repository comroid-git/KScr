using System;

namespace KScr.Lib.Bytecode
{
    [Flags]
    public enum MemberModifier {
        // accessibility keywords
        Public = 0x0000_1000,
        Internal = 0x0000_2000,
        Protected = 0x0000_4000,
        Private = 0x0000_8000,
        Abstract = 0x0040_0000,
        Final = 0x0080_0000,
        Static = 0x0010_0000
    }
    
    public interface IModifierContainer 
    {
        public MemberModifier Modifier { get; }
    }

    public static class ModifierMethods
    {
        public static bool IsPublic(this IModifierContainer container) => (container.Modifier & MemberModifier.Public) != 0;
        public static bool IsInternal(this IModifierContainer container) => (container.Modifier & MemberModifier.Internal) != 0;
        public static bool IsProtected(this IModifierContainer container) => (container.Modifier & MemberModifier.Protected) != 0;
        public static bool IsPrivate(this IModifierContainer container) => (container.Modifier & MemberModifier.Private) != 0;
        public static bool IsAbstract(this IModifierContainer container) => (container.Modifier & MemberModifier.Abstract) != 0;
        public static bool IsFinal(this IModifierContainer container) => (container.Modifier & MemberModifier.Final) != 0;
        public static bool IsStatic(this IModifierContainer container) => (container.Modifier & MemberModifier.Static) != 0;
    }
}