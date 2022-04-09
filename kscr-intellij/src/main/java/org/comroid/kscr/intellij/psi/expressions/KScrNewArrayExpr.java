package org.comroid.kscr.intellij.psi.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.psi.types.ArrayTypeImpl;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Optional;

public class KScrNewArrayExpr extends KScrExpression{
	
	public KScrNewArrayExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrTypeRef> elementType(){
		return PsiUtils.childOfType(this, KScrElement.class).flatMap(x -> PsiUtils.childOfType(x, KScrTypeRef.class));
	}
	
	public @Nullable JvmType type(){
		return elementType().map(KScrTypeRef::asType).map(ArrayTypeImpl::of).orElse(null);
	}
}