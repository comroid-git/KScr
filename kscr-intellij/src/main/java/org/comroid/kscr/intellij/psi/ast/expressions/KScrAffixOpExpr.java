package org.comroid.kscr.intellij.psi.ast.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Optional;

public class KScrAffixOpExpr extends KScrExpression{
	
	public KScrAffixOpExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public String operation(){
		if(isPostfix())
			return getLastChild().getText();
		else
			return getFirstChild().getText();
	}
	
	public boolean isPostfix(){
		return getFirstChild() instanceof KScrExpression;
	}
	
	public Optional<KScrExpression> expression(){
		return PsiUtils.childOfType(this, KScrExpression.class);
	}
	
	public @Nullable JvmType type(){
		return expression().map(KScrExpression::type).orElse(null);
	}
}