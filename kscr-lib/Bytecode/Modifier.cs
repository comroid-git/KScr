using System;
using KScr.Lib.Model;

namespace KScr.Lib.Bytecode
{
    [Flags]
    public enum MemberModifier : uint
    {
        None = 0,
        Public = 0x0000_1000,
        Internal = 0x0000_2000,
        Protected = 0x0000_4000,
        Private = 0x0000_8000,
        Abstract = 0x0040_0000,
        Final = 0x0080_0000,
        Static = 0x0010_0000
    }

    public static class ModifierMethods
    {
        public static bool IsPublic(this IModifierContainer container) => IsPublic(container.Modifier);

        public static bool IsInternal(this IModifierContainer container) => IsInternal(container.Modifier);

        public static bool IsProtected(this IModifierContainer container) => IsProtected(container.Modifier);

        public static bool IsPrivate(this IModifierContainer container) => IsPrivate(container.Modifier);

        public static bool IsAbstract(this IModifierContainer container) => IsAbstract(container.Modifier);

        public static bool IsFinal(this IModifierContainer container) => IsFinal(container.Modifier);

        public static bool IsStatic(this IModifierContainer container) => IsStatic(container.Modifier);

        public static bool IsPublic(this MemberModifier mod) => (mod & MemberModifier.Public) != 0;

        public static bool IsInternal(this MemberModifier mod) => (mod & MemberModifier.Internal) != 0;

        public static bool IsProtected(this MemberModifier mod) => (mod & MemberModifier.Protected) != 0;

        public static bool IsPrivate(this MemberModifier mod) => (mod & MemberModifier.Private) != 0;

        public static bool IsAbstract(this MemberModifier mod) => (mod & MemberModifier.Abstract) != 0;

        public static bool IsFinal(this MemberModifier mod) => (mod & MemberModifier.Final) != 0;

        public static bool IsStatic(this MemberModifier mod) => (mod & MemberModifier.Static) != 0;
    }

    public interface IModifierContainer
    {
        public MemberModifier Modifier { get; }
    }
}