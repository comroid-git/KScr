package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.jetbrains.annotations.NotNull;

public class KScrIdPart extends KScrAstElement{
	
	public KScrIdPart(@NotNull ASTNode node){
		super(node);
	}
}