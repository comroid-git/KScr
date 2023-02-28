package org.comroid.kscr.intellij.psi;

import com.intellij.psi.TokenType;
import com.intellij.psi.tree.IElementType;
import com.intellij.psi.tree.TokenSet;
import org.antlr.intellij.adaptor.lexer.PSIElementTypeFactory;
import org.antlr.intellij.adaptor.lexer.RuleIElementType;
import org.antlr.intellij.adaptor.lexer.TokenIElementType;
import org.comroid.kscr.intellij.KScrLanguage;

import java.util.List;

import static org.comroid.kscr.intellij.antlr_generated.KScrLexer.*;

public final class Tokens {
    public static IElementType BAD_TOKEN_TYPE = TokenType.BAD_CHARACTER;

    public static final List<TokenIElementType> TOKEN_ELEMENT_TYPES = PSIElementTypeFactory.getTokenIElementTypes(KScrLanguage.LANGUAGE);
    public static final List<RuleIElementType> RULE_ELEMENT_TYPES = PSIElementTypeFactory.getRuleIElementTypes(KScrLanguage.LANGUAGE);

    public static final TokenSet Comment = $(SING_COMMENT);
    public static final TokenSet PrimitiveType = $(
            TYPE, VOID, OBJECT, ARRAYIDENT, TUPLEIDENT, ENUM, ANNOTATION,
            BOOL, BYTE, SHORT, INT, LONG, FLOAT, DOUBLE, NUMIDENT);
    public static final TokenSet NumericLit = $(NUMLIT);
    public static final TokenSet StringLit = $(STRLIT);
    public static final TokenSet Literals = $(STDIOLIT, ENDLLIT, NULL, TRUE, FALSE);
    public static final TokenSet Braces = $(LPAREN, LSQUAR, LBRACE, LESSER, GREATER, RBRACE, RSQUAR, RPAREN);
    public static final TokenSet Infrastructure = $(SEMICOLON, COMMA,
            PUBLIC, PROTECTED, INTERNAL, PRIVATE,
            ABSTRACT, FINAL, SYNCHRONIZED, NATIVE, SERVE,
            WHERE, SELECT, ELSE,
            IF, FOR, FOREACH, WHILE, DO, TRY, CATCH, NEW,
            RETURN, THROW,
            CLASS, ENUM, INTERFACE, ANNOTATION);
    public static final TokenSet InfrastructureLow = $(COLON, DOT, LDASHARROW, LLDASHARROW, RRDASHARROW, RDASHARROW, RREQARROW, REQARROW);

    private static TokenSet $(int... types) {
        return PSIElementTypeFactory.createTokenSet(KScrLanguage.LANGUAGE, types);
    }
}
