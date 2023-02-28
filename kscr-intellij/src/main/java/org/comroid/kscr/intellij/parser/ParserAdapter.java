package org.comroid.kscr.intellij.parser;

import com.intellij.psi.tree.IElementType;
import org.antlr.intellij.adaptor.parser.ANTLRParserAdaptor;
import org.antlr.v4.runtime.Parser;
import org.antlr.v4.runtime.tree.ParseTree;
import org.comroid.kscr.intellij.KScrLanguage;
import org.comroid.kscr.intellij.antlr_generated.KScrParser;
import org.jetbrains.annotations.NotNull;

public class ParserAdapter extends ANTLRParserAdaptor {
    public ParserAdapter(@NotNull KScrParser parser) {
        super(KScrLanguage.LANGUAGE, parser);
    }

    @Override
    protected ParseTree parse(Parser parser, IElementType root) {
        return ((KScrParser)parser).file();
    }
}
