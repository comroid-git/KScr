package org.comroid.kscr.intellij.parser;

import com.intellij.lexer.Lexer;
import com.intellij.openapi.editor.colors.TextAttributesKey;
import com.intellij.openapi.fileTypes.SyntaxHighlighter;
import com.intellij.openapi.fileTypes.SyntaxHighlighterFactory;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.vfs.VirtualFile;
import com.intellij.psi.tree.IElementType;
import org.comroid.kscr.intellij.antlr_generated.KScrLangLexer;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class KScrSyntaxHighlighter extends SyntaxHighlighterFactory implements SyntaxHighlighter {

    public static final TextAttributesKey ID = TextAttributesKey.createTextAttributesKey("KScr-ID", DefaultLanguageHighlighterColors.IDENTIFIER);
    public static final TextAttributesKey KEYWORD = TextAttributesKey.createTextAttributesKey("KScr-Keyword", DefaultLanguageHighlighterColors.KEYWORD);
    public static final TextAttributesKey NUMLIT = TextAttributesKey.createTextAttributesKey("KScr-Number-Literal", DefaultLanguageHighlighterColors.NUMBER);
    public static final TextAttributesKey STRLIT = TextAttributesKey.createTextAttributesKey("KScr-String-Literal", DefaultLanguageHighlighterColors.STRING);
    public static final TextAttributesKey SYMBOLS = TextAttributesKey.createTextAttributesKey("KScr-Symbols", DefaultLanguageHighlighterColors.SEMICOLON);
    public static final TextAttributesKey DOTCOMMA = TextAttributesKey.createTextAttributesKey("KScr-Dot-Comma", DefaultLanguageHighlighterColors.DOT);
    public static final TextAttributesKey OPERATORS = TextAttributesKey.createTextAttributesKey("KScr-Operators", DefaultLanguageHighlighterColors.OPERATION_SIGN);
    public static final TextAttributesKey COMMENT = TextAttributesKey.createTextAttributesKey("KScr-Comment", DefaultLanguageHighlighterColors.LINE_COMMENT);

    @NotNull
    @Override public Lexer getHighlightingLexer(){
        KScrLangLexer lexer = new KScrLangLexer(null);
        return new LexerAdapter(lexer);
    }

    public TextAttributesKey @NotNull [] getTokenHighlights(IElementType element){
        if(Tokens.IDENTIFIERS.contains(element))
            return array(ID);
        else if(Tokens.KEYWORDS.contains(element))
            return array(KEYWORD);
        else if(Tokens.getFor(KScrLangLexer.STRLIT) == element)
            return array(STRLIT);
        else if(Tokens.LITERALS.contains(element))
            return array(NUMLIT); // Add boolean literal formatting
        else if(Tokens.PUNCTUATION.contains(element))
            return array(DOTCOMMA);
        else if(Tokens.SYMBOLS.contains(element))
            return array(SYMBOLS);
        else if(Tokens.OPERATORS.contains(element))
            return array(OPERATORS);
        else if(Tokens.COMMENTS.contains(element))
            return array(COMMENT);

        return new TextAttributesKey[0];
    }

    public @NotNull com.intellij.openapi.fileTypes.SyntaxHighlighter getSyntaxHighlighter(@Nullable Project project, @Nullable VirtualFile virtualFile){
        return new KScrSyntaxHighlighter();
    }

    public TextAttributesKey[] array(TextAttributesKey key){
        return new TextAttributesKey[]{key};
    }
}
