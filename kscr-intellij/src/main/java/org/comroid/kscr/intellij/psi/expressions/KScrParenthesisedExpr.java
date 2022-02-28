package org.comroid.kscr.intellij.psi.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.psi.KScrExpression;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class KScrParenthesisedExpr extends KScrExpression{
	
	public KScrParenthesisedExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public @Nullable JvmType type(){
		var parenthesised = PsiUtils.childOfType(this, KScrExpression.class);
		return parenthesised.map(KScrExpression::type).orElse(null);
	}
}