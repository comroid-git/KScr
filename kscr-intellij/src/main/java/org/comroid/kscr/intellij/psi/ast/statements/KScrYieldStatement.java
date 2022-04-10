package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.ast.expressions.KScrExpression;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.Optional;

public class KScrYieldStatement extends KScrAstElement implements KScrStatement{
	
	public KScrYieldStatement(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrExpression> yields(){
		return PsiUtils.childOfType(this, KScrExpression.class);
	}
}