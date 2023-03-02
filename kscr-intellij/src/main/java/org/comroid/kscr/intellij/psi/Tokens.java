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

    public static final int[] CommentTokens = new int[]{SING_COMMENT};
    public static final int[] PrimitiveTypeTokens = new int[]{TYPE, VOID, OBJECT, ARRAYIDENT, TUPLEIDENT, ENUM, ANNOTATION, BOOL, BYTE, SHORT, INT, LONG, FLOAT, DOUBLE, NUMIDENT};
    public static final int[] NumericLitTokens = new int[]{NUMLIT};
    public static final int[] StringLitTokens = new int[]{STRLIT};
    public static final int[] LiteralsTokens = new int[]{STDIOLIT, ENDLLIT, NULL, TRUE, FALSE};
    public static final int[] BracesTokens = new int[]{LPAREN, LSQUAR, LBRACE, LESSER, GREATER, RBRACE, RSQUAR, RPAREN};
    public static final int[] InfrastructureTokens = new int[]{SEMICOLON, COMMA, PUBLIC, PROTECTED, INTERNAL, PRIVATE, STATIC, ABSTRACT, FINAL, SYNCHRONIZED, NATIVE, SERVE, WHERE, SELECT, ELSE, IF, FOR, FOREACH, WHILE, DO, TRY, CATCH, NEW, RETURN, THROW, PACKAGE, IMPORT, CLASS, ENUM, INTERFACE, ANNOTATION};
    public static final int[] InfrastructureLowTokens = new int[]{COLON, DOT, LDASHARROW, LLDASHARROW, RRDASHARROW, RDASHARROW, RREQARROW, REQARROW};
    public static final TokenSet Comment = $(CommentTokens);
    public static final TokenSet PrimitiveType = $(PrimitiveTypeTokens);
    public static final TokenSet NumericLit = $(NumericLitTokens);
    public static final TokenSet StringLit = $(StringLitTokens);
    public static final TokenSet Literals = $(LiteralsTokens);
    public static final TokenSet Braces = $(BracesTokens);
    public static final TokenSet Infrastructure = $(InfrastructureTokens);
    public static final TokenSet InfrastructureLow = $(InfrastructureLowTokens);

    private static TokenSet $(int... types) {
        return PSIElementTypeFactory.createTokenSet(KScrLanguage.LANGUAGE, types);
    }
}
