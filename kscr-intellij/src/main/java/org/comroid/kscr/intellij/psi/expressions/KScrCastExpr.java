package org.comroid.kscr.intellij.psi.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.psi.KScrElement;
import org.comroid.kscr.intellij.psi.KScrExpression;
import org.comroid.kscr.intellij.psi.KScrTypeRef;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Optional;

public class KScrCastExpr extends KScrExpression{
	
	public KScrCastExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrTypeRef> castingTo(){
		return PsiUtils.childOfType(this, KScrElement.class).flatMap(x -> PsiUtils.childOfType(x, KScrTypeRef.class));
	}
	
	public @Nullable JvmType type(){
		return castingTo().map(KScrTypeRef::asType).orElse(null);
	}
}