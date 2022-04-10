package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.jetbrains.annotations.NotNull;

public class KScrVariableAssignment extends KScrAstElement{
	
	public KScrVariableAssignment(@NotNull ASTNode node){
		super(node);
	}
}