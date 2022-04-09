package org.comroid.kscr.intellij.psi.ast.common;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.ast.statements.KScrStatement;
import org.jetbrains.annotations.NotNull;

public class KScrInitialisation extends KScrAstElement implements KScrStatement{
	
	public KScrInitialisation(@NotNull ASTNode node){
		super(node);
	}
}