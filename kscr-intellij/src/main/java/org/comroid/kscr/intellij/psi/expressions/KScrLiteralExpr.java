package org.comroid.kscr.intellij.psi.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiType;
import org.comroid.kscr.intellij.psi.Tokens;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class KScrLiteralExpr extends KScrExpression{
	
	public KScrLiteralExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public @Nullable JvmType type(){
		if(getNode().findChildByType(Tokens.TOK_NULL) != null)
			return PsiType.NULL;
		if(getNode().findChildByType(Tokens.TOK_BOOLLIT) != null)
			return PsiType.BOOLEAN;
		// TODO: implicit conversions
		if(getNode().findChildByType(Tokens.TOK_NUMLIT) != null)
			return PsiType.INT;
		if(getNode().findChildByType(Tokens.TOK_RANGELIT) != null)
			return PsiType.DOUBLE;
		return null;
	}
}