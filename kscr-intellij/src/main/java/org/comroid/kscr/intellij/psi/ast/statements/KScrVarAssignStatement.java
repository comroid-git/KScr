package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.jetbrains.annotations.NotNull;

public class KScrVarAssignStatement extends KScrAstElement implements KScrStatement{
	
	public KScrVarAssignStatement(@NotNull ASTNode node){
		super(node);
	}
}