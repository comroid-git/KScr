package org.comroid.kscr.intellij.psi.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Optional;

public class KScrAssignExpr extends KScrExpression{
	
	public KScrAssignExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrExpression> expression(){
		return PsiUtils.childOfType(this, KScrExpression.class);
	}
	
	public @Nullable JvmType type(){
		return expression().map(KScrExpression::type).orElse(null);
	}
}