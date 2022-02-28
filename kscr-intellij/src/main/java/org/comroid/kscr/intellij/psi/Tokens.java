package org.comroid.kscr.intellij.psi;

import com.intellij.psi.TokenType;
import com.intellij.psi.tree.IElementType;
import com.intellij.psi.tree.TokenSet;
import org.antlr.intellij.adaptor.lexer.PSIElementTypeFactory;
import org.antlr.intellij.adaptor.lexer.RuleIElementType;
import org.antlr.intellij.adaptor.lexer.TokenIElementType;
import org.comroid.kscr.intellij.KScrLanguage;
import org.comroid.kscr.intellij.antlr_generated.KScrLangLexer;
import org.comroid.kscr.intellij.antlr_generated.KScrLangParser;
import org.jetbrains.annotations.Contract;

import static org.antlr.intellij.adaptor.lexer.PSIElementTypeFactory.createTokenSet;

import java.util.List;

public class Tokens {
    public static IElementType BAD_TOKEN_TYPE = TokenType.BAD_CHARACTER;

    public static final List<TokenIElementType> TOKEN_ELEMENT_TYPES = PSIElementTypeFactory.getTokenIElementTypes(KScrLanguage.LANGUAGE);
    public static final List<RuleIElementType> RULE_ELEMENT_TYPES = PSIElementTypeFactory.getRuleIElementTypes(KScrLanguage.LANGUAGE);

    public static final TokenSet KEYWORDS = createTokenSet(
            KScrLanguage.LANGUAGE,

            KScrLangLexer.BOOL,
            KScrLangLexer.BYTE,
            KScrLangLexer.SHORT,
            KScrLangLexer.CHAR,
            KScrLangLexer.INT,
            KScrLangLexer.LONG,
            KScrLangLexer.FLOAT,
            KScrLangLexer.DOUBLE,
            KScrLangLexer.VOID,

            KScrLangLexer.VAR,

            KScrLangLexer.PRIVATE,
            KScrLangLexer.PROTECTED,
            KScrLangLexer.PUBLIC,

            KScrLangLexer.FINAL,
            KScrLangLexer.STATIC,
            KScrLangLexer.ABSTRACT,
            KScrLangLexer.SYNCHRONISED,
            KScrLangLexer.NATIVE,

            KScrLangLexer.CLASS,
            KScrLangLexer.INTERFACE,
            KScrLangLexer.ENUM,
            KScrLangLexer.AT,
            KScrLangLexer.RECORD,
            KScrLangLexer.SINGLE,

            KScrLangLexer.IMPORT,
            KScrLangLexer.PACKAGE,
            KScrLangLexer.EXTENDS,
            KScrLangLexer.IMPLEMENTS,
            KScrLangLexer.PERMITS,

            KScrLangLexer.RETURN,
            KScrLangLexer.THROW,
            KScrLangLexer.NEW,
            KScrLangLexer.ASSERT,

            KScrLangLexer.THIS,

            KScrLangLexer.SWITCH,
            KScrLangLexer.DEFAULT,
            KScrLangLexer.CASE,
            KScrLangLexer.WHILE,
            KScrLangLexer.DO,
            KScrLangLexer.IF,
            KScrLangLexer.FOR,
            KScrLangLexer.ELSE,

            KScrLangLexer.NULL,
            KScrLangLexer.TRUE,
            KScrLangLexer.FALSE
    );

    public static final TokenSet OPERATORS = createTokenSet(
            KScrLanguage.LANGUAGE,

            KScrLangLexer.EQUAL,
            KScrLangLexer.INEQUAL,
            KScrLangLexer.GREATEREQ,
            KScrLangLexer.LESSEREQ,
            KScrLangLexer.GREATER,
            KScrLangLexer.LESSER,

            KScrLangLexer.AND,
            KScrLangLexer.OR,
            KScrLangLexer.BITAND,
            KScrLangLexer.BITOR,

            KScrLangLexer.UP,

            KScrLangLexer.PLUSPLUS,
            KScrLangLexer.MINUSMINUS,

            KScrLangLexer.STAR,
            KScrLangLexer.SLASH,
            KScrLangLexer.PLUS,
            KScrLangLexer.MINUS,
            KScrLangLexer.PERCENT
    );

    // used for syntax highlighting
    public static final TokenSet LITERALS = createTokenSet(
            KScrLanguage.LANGUAGE,

            KScrLangLexer.NUMLIT,
            KScrLangLexer.BOOLLIT,
            KScrLangLexer.STRLIT,
            KScrLangLexer.RANGELIT
    );

    public static final TokenSet SYMBOLS = createTokenSet(
            KScrLanguage.LANGUAGE,

            KScrLangLexer.QUOTE,
            KScrLangLexer.SEMICOLON,

            KScrLangLexer.RDASHARROW,
            KScrLangLexer.LDASHARROW,
            KScrLangLexer.DEQARROW,
            KScrLangLexer.REQARROW
    );

    public static final TokenSet PUNCTUATION = createTokenSet(
            KScrLanguage.LANGUAGE,

            KScrLangLexer.DOT,
            KScrLangLexer.COMMA,
            KScrLangLexer.COLON,
            KScrLangLexer.ASSIGN,

            KScrLangLexer.EXCLAMATION,
            KScrLangLexer.QUESTION
    );

    public static final TokenSet WHITESPACES = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.WS);
    public static final TokenSet COMMENTS = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.SING_COMMENT);
    public static final TokenSet STRING_LITERALS = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.STRLIT);
    public static final TokenSet IDENTIFIERS = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.ID);

    // single-element sets
    public static final TokenSet RULE_IMPORT = TokenSet.create(getRuleFor(KScrLangParser.RULE_importDecl));
    public static final TokenSet RULE_BIN_OP = TokenSet.create(getRuleFor(KScrLangParser.RULE_binaryop));
    public static final TokenSet RULE_CALL = TokenSet.create(getRuleFor(KScrLangParser.RULE_call));
    public static final TokenSet RULE_ID_PART = TokenSet.create(getRuleFor(KScrLangParser.RULE_idPart));
    public static final TokenSet RULE_INITIALISATION = TokenSet.create(getRuleFor(KScrLangParser.RULE_initialisation));
    public static final TokenSet RULE_CAST = TokenSet.create(getRuleFor(KScrLangParser.RULE_cast));
    public static final TokenSet RULE_NEW_ARRAY = TokenSet.create(getRuleFor(KScrLangParser.RULE_newArray));
    public static final TokenSet RULE_NEW_LIST_ARRAY = TokenSet.create(getRuleFor(KScrLangParser.RULE_newListedArray));

    public static final TokenSet TOK_ASSIGN = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.ASSIGN);
    public static final TokenSet TOK_INSTANCEOF = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.INSTANCEOF);
    public static final TokenSet TOK_THIS = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.THIS);
    public static final TokenSet TOK_STRING = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.STRLIT);

    public static final TokenSet TOK_CLASS = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.CLASS);
    public static final TokenSet TOK_INTERFACE = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.INTERFACE);
    public static final TokenSet TOK_ANNOTATION = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.ANNOTATION);
    public static final TokenSet TOK_AT = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.AT);
    public static final TokenSet TOK_ENUM = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.ENUM);
    public static final TokenSet TOK_RECORD = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.RECORD);
    public static final TokenSet TOK_SINGLE = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.SINGLE);

    public static final TokenSet TOK_NULL = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.NULL);
    public static final TokenSet TOK_NUMLIT = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.NUMLIT);
    public static final TokenSet TOK_RANGELIT = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.RANGELIT);
    public static final TokenSet TOK_BOOLLIT = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.BOOLLIT);

    public static final TokenSet PARENTHESIS = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.LPAREN, KScrLangLexer.RPAREN);
    public static final TokenSet SQ_BRACES = createTokenSet(KScrLanguage.LANGUAGE, KScrLangLexer.LSQUAR, KScrLangLexer.RSQUAR);
    public static final TokenSet PRE_POST_OPS = TokenSet.create(
            getRuleFor(KScrLangParser.RULE_prefixop), getRuleFor(KScrLangParser.RULE_postfixop));

    // used for literal expression checking
    public static final TokenSet SEM_LITERALS = createTokenSet(KScrLanguage.LANGUAGE,
            KScrLangLexer.NULL,
            KScrLangLexer.NUMLIT,
            KScrLangLexer.RANGELIT,
            KScrLangLexer.BOOLLIT
    );

    @Contract(pure = true) public static IElementType getFor(int type){
        return TOKEN_ELEMENT_TYPES.get(type);
    }

    @Contract(pure = true) public static IElementType getRuleFor(int type){
        return RULE_ELEMENT_TYPES.get(type);
    }
}
