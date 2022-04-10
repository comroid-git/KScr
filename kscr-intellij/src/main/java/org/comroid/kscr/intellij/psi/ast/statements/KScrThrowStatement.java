package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.ast.expressions.KScrExpression;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.Optional;

public class KScrThrowStatement extends KScrAstElement implements KScrStatement{
	
	public KScrThrowStatement(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrExpression> returns(){
		return PsiUtils.childOfType(this, KScrExpression.class);
	}
}