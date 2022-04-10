package org.comroid.kscr.intellij.psi.ast.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import static org.comroid.kscr.intellij.psi.utils.JvmClassUtils.typeByName;

public class KScrStringLiteralExpr extends KScrExpression{
	
	public KScrStringLiteralExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public @Nullable JvmType type(){
		return typeByName("java.lang.String", getProject());
	}
}