package org.comroid.kscr.intellij.psi;

import com.intellij.psi.TokenType;
import com.intellij.psi.tree.IElementType;
import com.intellij.psi.tree.TokenSet;
import org.antlr.intellij.adaptor.lexer.PSIElementTypeFactory;
import org.antlr.intellij.adaptor.lexer.RuleIElementType;
import org.antlr.intellij.adaptor.lexer.TokenIElementType;
import org.comroid.kscr.intellij.KScrLanguage;
import org.comroid.kscr.intellij.antlr_generated.KScrLexer;
import org.comroid.kscr.intellij.antlr_generated.KScrParser;
import org.jetbrains.annotations.Contract;

import static org.antlr.intellij.adaptor.lexer.PSIElementTypeFactory.createTokenSet;

import java.util.List;

public class Tokens {
    public static IElementType BAD_TOKEN_TYPE = TokenType.BAD_CHARACTER;

    public static final List<TokenIElementType> TOKEN_ELEMENT_TYPES = PSIElementTypeFactory.getTokenIElementTypes(KScrLanguage.LANGUAGE);
    public static final List<RuleIElementType> RULE_ELEMENT_TYPES = PSIElementTypeFactory.getRuleIElementTypes(KScrLanguage.LANGUAGE);

    public static final TokenSet KEYWORDS = createTokenSet(
            KScrLanguage.LANGUAGE,

            KScrLexer.BOOL,
            KScrLexer.BYTE,
            KScrLexer.SHORT,
            KScrLexer.CHAR,
            KScrLexer.INT,
            KScrLexer.LONG,
            KScrLexer.FLOAT,
            KScrLexer.DOUBLE,
            KScrLexer.VOID,

            KScrLexer.VAR,

            KScrLexer.PRIVATE,
            KScrLexer.PROTECTED,
            KScrLexer.PUBLIC,

            KScrLexer.FINAL,
            KScrLexer.STATIC,
            KScrLexer.ABSTRACT,
            KScrLexer.SYNCHRONISED,
            KScrLexer.NATIVE,

            KScrLexer.CLASS,
            KScrLexer.INTERFACE,
            KScrLexer.ENUM,
            KScrLexer.AT,
            KScrLexer.RECORD,
            KScrLexer.SINGLE,

            KScrLexer.IMPORT,
            KScrLexer.PACKAGE,
            KScrLexer.EXTENDS,
            KScrLexer.IMPLEMENTS,
            KScrLexer.PERMITS,

            KScrLexer.RETURN,
            KScrLexer.THROW,
            KScrLexer.NEW,
            KScrLexer.ASSERT,

            KScrLexer.THIS,

            KScrLexer.SWITCH,
            KScrLexer.DEFAULT,
            KScrLexer.CASE,
            KScrLexer.WHILE,
            KScrLexer.DO,
            KScrLexer.IF,
            KScrLexer.FOR,
            KScrLexer.ELSE,

            KScrLexer.NULL,
            KScrLexer.TRUE,
            KScrLexer.FALSE
    );

    public static final TokenSet OPERATORS = createTokenSet(
            KScrLanguage.LANGUAGE,

            KScrLexer.EQUAL,
            KScrLexer.INEQUAL,
            KScrLexer.GREATEREQ,
            KScrLexer.LESSEREQ,
            KScrLexer.GREATER,
            KScrLexer.LESSER,

            KScrLexer.AND,
            KScrLexer.OR,
            KScrLexer.BITAND,
            KScrLexer.BITOR,

            KScrLexer.UP,

            KScrLexer.PLUSPLUS,
            KScrLexer.MINUSMINUS,

            KScrLexer.STAR,
            KScrLexer.SLASH,
            KScrLexer.PLUS,
            KScrLexer.MINUS,
            KScrLexer.PERCENT
    );

    // used for syntax highlighting
    public static final TokenSet LITERALS = createTokenSet(
            KScrLanguage.LANGUAGE,

            KScrLexer.NUMLIT,
            KScrLexer.BOOLLIT,
            KScrLexer.STRLIT,
            KScrLexer.RANGELIT
    );

    public static final TokenSet SYMBOLS = createTokenSet(
            KScrLanguage.LANGUAGE,

            KScrLexer.QUOTE,
            KScrLexer.SEMICOLON,

            KScrLexer.RDASHARROW,
            KScrLexer.LDASHARROW,
            KScrLexer.DEQARROW,
            KScrLexer.REQARROW
    );

    public static final TokenSet PUNCTUATION = createTokenSet(
            KScrLanguage.LANGUAGE,

            KScrLexer.DOT,
            KScrLexer.COMMA,
            KScrLexer.COLON,
            KScrLexer.ASSIGN,

            KScrLexer.EXCLAMATION,
            KScrLexer.QUESTION
    );

    public static final TokenSet WHITESPACES = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.WS);
    public static final TokenSet COMMENTS = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.SING_COMMENT);
    public static final TokenSet STRING_LITERALS = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.STRLIT);
    public static final TokenSet IDENTIFIERS = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.ID);

    // single-element sets
    public static final TokenSet RULE_IMPORT = TokenSet.create(getRuleFor(KScrParser.RULE_importDecl));
    public static final TokenSet RULE_BIN_OP = TokenSet.create(getRuleFor(KScrParser.RULE_binaryop));
    public static final TokenSet RULE_CALL = TokenSet.create(getRuleFor(KScrParser.RULE_call));
    public static final TokenSet RULE_ID_PART = TokenSet.create(getRuleFor(KScrParser.RULE_idPart));
    public static final TokenSet RULE_INITIALISATION = TokenSet.create(getRuleFor(KScrParser.RULE_initialisation));
    public static final TokenSet RULE_CAST = TokenSet.create(getRuleFor(KScrParser.RULE_cast));
    public static final TokenSet RULE_NEW_ARRAY = TokenSet.create(getRuleFor(KScrParser.RULE_newArray));
    public static final TokenSet RULE_NEW_LIST_ARRAY = TokenSet.create(getRuleFor(KScrParser.RULE_newListedArray));

    public static final TokenSet TOK_ASSIGN = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.ASSIGN);
    public static final TokenSet TOK_INSTANCEOF = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.INSTANCEOF);
    public static final TokenSet TOK_THIS = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.THIS);
    public static final TokenSet TOK_STRING = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.STRLIT);

    public static final TokenSet TOK_CLASS = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.CLASS);
    public static final TokenSet TOK_INTERFACE = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.INTERFACE);
    public static final TokenSet TOK_ANNOTATION = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.ANNOTATION);
    public static final TokenSet TOK_AT = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.AT);
    public static final TokenSet TOK_ENUM = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.ENUM);
    public static final TokenSet TOK_RECORD = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.RECORD);
    public static final TokenSet TOK_SINGLE = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.SINGLE);

    public static final TokenSet TOK_NULL = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.NULL);
    public static final TokenSet TOK_NUMLIT = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.NUMLIT);
    public static final TokenSet TOK_RANGELIT = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.RANGELIT);
    public static final TokenSet TOK_BOOLLIT = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.BOOLLIT);

    public static final TokenSet PARENTHESIS = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.LPAREN, KScrLexer.RPAREN);
    public static final TokenSet SQ_BRACES = createTokenSet(KScrLanguage.LANGUAGE, KScrLexer.LSQUAR, KScrLexer.RSQUAR);
    public static final TokenSet PRE_POST_OPS = TokenSet.create(
            getRuleFor(KScrParser.RULE_prefixop), getRuleFor(KScrParser.RULE_postfixop));

    // used for literal expression checking
    public static final TokenSet SEM_LITERALS = createTokenSet(KScrLanguage.LANGUAGE,
            KScrLexer.NULL,
            KScrLexer.NUMLIT,
            KScrLexer.RANGELIT,
            KScrLexer.BOOLLIT
    );

    @Contract(pure = true) public static IElementType getFor(int type){
        return TOKEN_ELEMENT_TYPES.get(type);
    }

    @Contract(pure = true) public static IElementType getRuleFor(int type){
        return RULE_ELEMENT_TYPES.get(type);
    }
}
