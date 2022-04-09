package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.jetbrains.annotations.NotNull;

public class KScrBreakStatement extends KScrAstElement implements KScrStatement{
	
	public KScrBreakStatement(@NotNull ASTNode node){
		super(node);
	}
}