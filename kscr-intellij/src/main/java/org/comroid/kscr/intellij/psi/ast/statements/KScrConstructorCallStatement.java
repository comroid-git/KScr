package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.jetbrains.annotations.NotNull;

public class KScrConstructorCallStatement extends KScrAstElement implements KScrStatement{
	
	public KScrConstructorCallStatement(@NotNull ASTNode node){
		super(node);
	}
}