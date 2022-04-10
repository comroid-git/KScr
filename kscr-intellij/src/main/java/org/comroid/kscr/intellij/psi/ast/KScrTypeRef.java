package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.Tokens;
import org.comroid.kscr.intellij.psi.types.ArrayTypeImpl;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class KScrTypeRef extends KScrAstElement{
	
	public KScrTypeRef(@NotNull ASTNode node){
		super(node);
	}
	
	@Nullable
	public JvmType asType(){
		if(getNode().findChildByType(Tokens.SQ_BRACES) != null)
			return PsiUtils.childOfType(this, KScrTypeRef.class)
					.map(KScrTypeRef::asType)
					.map(ArrayTypeImpl::of)
					.orElse(null);
		return PsiUtils.childOfType(this, KScrRawTypeRef.class)
				.map(KScrRawTypeRef::type).orElse(null);
	}
	
	@Nullable
	public JvmClass asClass(){
		return JvmClassUtils.asClass(asType());
	}
}