package org.comroid.kscr.intellij.psi.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmArrayType;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Optional;

public class KScrArrayIndexExpr extends KScrExpression{
	
	public KScrArrayIndexExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrExpression> arrayExpr(){
		return PsiUtils.childOfType(this, KScrExpression.class, 0);
	}
	
	public Optional<KScrExpression> indexExpr(){
		return PsiUtils.childOfType(this, KScrExpression.class, 1);
	}
	
	public @Nullable JvmType type(){
		return arrayExpr()
				.map(KScrExpression::type)
				.map(x -> x instanceof JvmArrayType ? x : null)
				.map(x-> ((JvmArrayType)x).getComponentType())
				.orElse(null);
	}
}