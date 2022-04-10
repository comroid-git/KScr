package org.comroid.kscr.intellij.psi.ast.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.psi.ast.KScrTypeRef;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Optional;

public class KScrInitialisationExpr extends KScrExpression{
	
	public KScrInitialisationExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrTypeRef> initialising(){
		return PsiUtils.childOfType(this, KScrElement.class).flatMap(x -> PsiUtils.childOfType(x, KScrTypeRef.class));
	}
	
	public @Nullable JvmType type(){
		return initialising().map(KScrTypeRef::asType).orElse(null);
	}
}