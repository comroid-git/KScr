package org.comroid.kscr.intellij.psi.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.util.PsiTreeUtil;
import org.comroid.kscr.intellij.psi.utils.JvmClassUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class KScrThisExpr extends KScrExpression{
	
	public KScrThisExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public @Nullable JvmType type(){
		var method = PsiTreeUtil.getParentOfType(this, KScrMethod.class);
		if(method == null || method.isStatic())
			return null;
		return JvmClassUtils.asType(method.containingType());
	}
}