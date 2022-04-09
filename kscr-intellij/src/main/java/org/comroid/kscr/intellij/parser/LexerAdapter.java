package org.comroid.kscr.intellij.parser;

import com.intellij.lang.Language;
import com.intellij.lexer.Lexer;
import com.intellij.lexer.LexerPosition;
import com.intellij.psi.tree.IElementType;
import org.antlr.intellij.adaptor.lexer.ANTLRLexerAdaptor;
import org.comroid.kscr.intellij.KScrLanguage;
import org.comroid.kscr.intellij.antlr_generated.KScrLexer;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class LexerAdapter extends ANTLRLexerAdaptor {
    /**
     * Constructs a new instance of {@link ANTLRLexerAdaptor} with
     * the specified {@link Language} and underlying ANTLR {@link
     * Lexer}.
     *
     * @param lexer    The underlying ANTLR lexer.
     */
    public LexerAdapter(org.antlr.v4.runtime.Lexer lexer) {
        super(KScrLanguage.LANGUAGE, lexer);
    }
}
