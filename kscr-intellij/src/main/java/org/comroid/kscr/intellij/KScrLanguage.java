package org.comroid.kscr.intellij;

import com.intellij.lang.Language;

public class KScrLanguage extends Language{
	
	public static final KScrLanguage LANGUAGE = new KScrLanguage();
	
	protected KScrLanguage(){
		super("KScr");
	}
}