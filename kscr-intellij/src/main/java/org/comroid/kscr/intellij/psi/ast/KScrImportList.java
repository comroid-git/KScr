package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.jetbrains.annotations.NotNull;

public class KScrImportList extends KScrAstElement{
	
	public KScrImportList(@NotNull ASTNode node){
		super(node);
	}
}