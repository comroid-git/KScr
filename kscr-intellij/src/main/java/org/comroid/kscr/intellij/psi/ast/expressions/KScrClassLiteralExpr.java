package org.comroid.kscr.intellij.psi.ast.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiReference;
import org.comroid.kscr.intellij.psi.KScrIdHolder;
import org.comroid.kscr.intellij.psi.utils.KScrTypeReference;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import static org.comroid.kscr.intellij.psi.utils.JvmClassUtils.typeByName;

public class KScrClassLiteralExpr extends KScrExpression implements KScrIdHolder{
	
	public KScrClassLiteralExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public @Nullable JvmType type(){
		return typeByName("java.lang.Class", getProject());
	}
	
	public PsiReference getReference(){
		return getIdElement().map(id -> new KScrTypeReference(id, this)).orElse(null);
	}
}