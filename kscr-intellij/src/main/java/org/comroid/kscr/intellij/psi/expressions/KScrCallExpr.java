package org.comroid.kscr.intellij.psi.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmMethod;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.antlr_generated.KScrParser;
import org.comroid.kscr.intellij.psi.Tokens;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Optional;

public class KScrCallExpr extends KScrExpression{
	
	public KScrCallExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrCall> call(){
		return PsiUtils.childOfType(this, KScrCall.class);
	}
	
	public Optional<KScrExpression> on(){
		return PsiUtils.childOfType(this, KScrExpression.class);
	}
	
	public boolean isSuperCall(){
		return !PsiUtils.matchingChildren(this, k -> k.getNode().getElementType() == Tokens.getFor(KScrParser.SUPER)).isEmpty();
	}
	
	public @Nullable JvmType type(){
		return call().map(KScrCall::resolveMethod).map(JvmMethod::getReturnType).orElse(null);
	}
}