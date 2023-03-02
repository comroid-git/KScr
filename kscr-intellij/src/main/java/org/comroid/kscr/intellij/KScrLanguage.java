package org.comroid.kscr.intellij;

import com.intellij.lang.Language;
import org.antlr.intellij.adaptor.lexer.PSIElementTypeFactory;
import org.comroid.kscr.intellij.antlr_generated.KScrLexer;
import org.comroid.kscr.intellij.antlr_generated.KScrParser;

public class KScrLanguage extends Language{
	public static final KScrLanguage LANGUAGE = new KScrLanguage();
	
	private KScrLanguage(){
		super("KScr");
		PSIElementTypeFactory.defineLanguageIElementTypes(this, KScrLexer.ruleNames, KScrParser.ruleNames);
	}
}