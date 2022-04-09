package org.comroid.kscr.intellij.parser;

import com.intellij.lang.Language;
import com.intellij.lang.PsiParser;
import com.intellij.psi.tree.IElementType;
import org.antlr.intellij.adaptor.parser.ANTLRParserAdaptor;
import org.antlr.v4.runtime.Parser;
import org.antlr.v4.runtime.tree.ParseTree;
import org.comroid.kscr.intellij.KScrLanguage;
import org.comroid.kscr.intellij.antlr_generated.KScrParser;

public class ParserAdapter extends ANTLRParserAdaptor {
    /**
     * Create a jetbrains adaptor for an ANTLR parser object. When
     * the IDE requests a {@link #parse(IElementType, PsiBuilder)},
     * the token stream will be set on the parser.
     *
     * @param parser
     */
    public ParserAdapter() {
        super(KScrLanguage.LANGUAGE, new KScrParser(null));
    }

    @Override
    protected ParseTree parse(Parser parser, IElementType root) {
        return null;
    }
}
