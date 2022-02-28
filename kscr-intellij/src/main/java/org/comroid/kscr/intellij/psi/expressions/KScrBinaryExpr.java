package org.comroid.kscr.intellij.psi.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import org.comroid.kscr.intellij.psi.KScrBinaryOp;
import org.comroid.kscr.intellij.psi.KScrExpression;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.Optional;

public class KScrBinaryExpr extends KScrExpression{
	
	public KScrBinaryExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrBinaryOp> op(){
		return PsiUtils.childOfType(this, KScrBinaryOp.class);
	}
	
	public String symbol(){
		return op().map(PsiElement::getText).orElse("");
	}
	
	public Optional<KScrExpression> left(){
		return PsiUtils.childOfType(this, KScrExpression.class, 0);
	}
	
	public Optional<KScrExpression> right(){
		return PsiUtils.childOfType(this, KScrExpression.class, 1);
	}
}