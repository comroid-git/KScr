using System;
using System.Collections.Generic;

namespace KScr.Lib.Model
{
    public interface IStatementComponent
    {
        public StatementComponentType Type { get; }
    }

    public interface ICompiler
    {
        public StatementComponentType OutputType { get; }

        // old method
        public Bytecode Compile(RuntimeBase runtime, IList<Token> tokens);

        public ICompiler AcceptToken(RuntimeBase vm, ref Bytecode bytecode, IList<Token> tokens, ref int i);
        public IStatementComponent Compose(RuntimeBase runtime);
        public BytecodePacket Compile(RuntimeBase runtime);
    }

    [Flags]
    public enum StatementComponentType : byte
    {
        // basetypes

        // basic compile-constant expression:
        // - 5              (-> standard numeric literal)
        // - "hello world"  (-> standard string literal)
        Expression = 0x10,

        // basic declaration:
        // - num x          (-> standard numeric declaration)
        // - str v          (-> standard string declaration)
        Declaration = 0x20,

        // pipe base node
        // - pipe<num> x    (-> standard pipe declaration)
        Pipe = 0x40 | Declaration,

        // advanced expressions

        // operators; used for arithmetic sorting
        Operator = 0x01 | Expression,

        // non-constant expression:
        // - (int) x              (-> cast)
        // - str.length()         (-> call)
        // - valueName            (-> read)
        Provider = 0x02 | Expression,

        // pipe-related symbols

        // providing pipe:
        // - [pipe] <== [provider OR emitter]  (-> standard pipe constructor)
        // - [pipe] <== [expression]           (-> pipe invoker)
        Consumer = 0x04 | Pipe,

        // consuming pipe:
        // - [pipe] ==> [consumer]                    (-> standard pipe handler)
        // - [pipe] where( [lambda<in, bool>] )       (-> filtering pipe stage)
        // - [pipe] select( [lambda<in, out>] )       (-> remapping pipe stage)
        Emitter = 0x08 | Provider | Pipe,

        // lambda pipe:
        // - Type::getName                (-> static lambda call)
        // - str::length                  (-> lambda call)
        // - it -> it.length()            (-> lambda)
        Lambda = 0x0F | Consumer | Emitter
    }
}