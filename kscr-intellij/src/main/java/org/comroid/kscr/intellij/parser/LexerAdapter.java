package org.comroid.kscr.intellij.parser;

import org.antlr.intellij.adaptor.lexer.ANTLRLexerAdaptor;
import org.antlr.v4.runtime.Lexer;
import org.comroid.kscr.intellij.KScrLanguage;
import org.jetbrains.annotations.NotNull;

public class LexerAdapter extends ANTLRLexerAdaptor {
    public LexerAdapter(@NotNull Lexer lexer) {
        super(KScrLanguage.LANGUAGE, lexer);
    }
}
