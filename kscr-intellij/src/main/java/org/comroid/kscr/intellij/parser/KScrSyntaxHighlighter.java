package org.comroid.kscr.intellij.parser;

import com.intellij.lexer.Lexer;
import com.intellij.openapi.editor.DefaultLanguageHighlighterColors;
import com.intellij.openapi.editor.colors.TextAttributesKey;
import com.intellij.openapi.fileTypes.SyntaxHighlighter;
import com.intellij.openapi.fileTypes.SyntaxHighlighterFactory;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.vfs.VirtualFile;
import com.intellij.psi.tree.IElementType;
import org.antlr.v4.runtime.ANTLRFileStream;
import org.antlr.v4.runtime.CharStream;
import org.antlr.v4.runtime.UnbufferedCharStream;
import org.comroid.kscr.intellij.KScrLanguage;
import org.comroid.kscr.intellij.antlr_generated.KScrLexer;
import org.comroid.kscr.intellij.psi.Tokens;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.io.ByteArrayInputStream;
import java.io.IOException;

public class KScrSyntaxHighlighter extends SyntaxHighlighterFactory {
    private static class Impl implements SyntaxHighlighter {
        private static final TextAttributesKey[] Comment = $(KScrSyntaxHighlighter.Comment);
        private static final TextAttributesKey[] PrimitiveType = $(KScrSyntaxHighlighter.PrimitiveType);
        private static final TextAttributesKey[] NumericLit = $(KScrSyntaxHighlighter.NumericLit);
        private static final TextAttributesKey[] StringLit = $(KScrSyntaxHighlighter.StringLit);
        private static final TextAttributesKey[] Literals = $(KScrSyntaxHighlighter.Literals);
        private static final TextAttributesKey[] Braces = $(KScrSyntaxHighlighter.Braces);
        private static final TextAttributesKey[] Infrastructure = $(KScrSyntaxHighlighter.Infrastructure);
        private static final TextAttributesKey[] InfrastructureLow = $(KScrSyntaxHighlighter.InfrastructureLow);
        private final KScrLexer lexer;

        public Impl(KScrLexer lexer) {
            this.lexer = lexer;
        }

        @Override
        public @NotNull Lexer getHighlightingLexer() {
            return new LexerAdapter(lexer);
        }

        @Override
        public TextAttributesKey @NotNull [] getTokenHighlights(IElementType element){
            if(Tokens.Comment.contains(element)) return Comment;
            else if(Tokens.PrimitiveType.contains(element)) return PrimitiveType;
            else if(Tokens.NumericLit.contains(element)) return NumericLit;
            else if(Tokens.StringLit.contains(element)) return StringLit;
            else if(Tokens.Literals.contains(element)) return Literals;
            else if(Tokens.Braces.contains(element)) return Braces;
            else if(Tokens.Infrastructure.contains(element)) return Infrastructure;
            else if(Tokens.InfrastructureLow.contains(element)) return InfrastructureLow;
            else return new TextAttributesKey[0];
        }
    }

    public static final TextAttributesKey Comment = TextAttributesKey.createTextAttributesKey("kscr_comment", DefaultLanguageHighlighterColors.LINE_COMMENT);
    public static final TextAttributesKey PrimitiveType = TextAttributesKey.createTextAttributesKey("kscr_primitive", DefaultLanguageHighlighterColors.KEYWORD);
    public static final TextAttributesKey NumericLit = TextAttributesKey.createTextAttributesKey("kscr_number", DefaultLanguageHighlighterColors.NUMBER);
    public static final TextAttributesKey StringLit = TextAttributesKey.createTextAttributesKey("kscr_string", DefaultLanguageHighlighterColors.STRING);
    public static final TextAttributesKey Literals = TextAttributesKey.createTextAttributesKey("kscr_literal", DefaultLanguageHighlighterColors.IDENTIFIER);
    public static final TextAttributesKey Braces = TextAttributesKey.createTextAttributesKey("kscr_brackets", DefaultLanguageHighlighterColors.BRACES);
    public static final TextAttributesKey Infrastructure = TextAttributesKey.createTextAttributesKey("kscr_infra", DefaultLanguageHighlighterColors.KEYWORD);
    public static final TextAttributesKey InfrastructureLow = TextAttributesKey.createTextAttributesKey("kscr_infra_low", DefaultLanguageHighlighterColors.IDENTIFIER);

    public @NotNull com.intellij.openapi.fileTypes.SyntaxHighlighter getSyntaxHighlighter(@Nullable Project project, @Nullable VirtualFile virtualFile){
        try {
            if (virtualFile == null)
                throw new NullPointerException("Need input to get syntaxHighlighter");
            return new Impl(new KScrLexer(new UnbufferedCharStream(new ByteArrayInputStream(virtualFile.contentsToByteArray()))));
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    private static TextAttributesKey[] $(TextAttributesKey key){
        return new TextAttributesKey[]{key};
    }
}
