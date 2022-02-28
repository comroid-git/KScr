package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.psi.types.ArrayTypeImpl;
import org.comroid.kscr.intellij.psi.utils.JvmClassUtils;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

public class KScrTypeRef extends KScrElement{
	
	public KScrTypeRef(@NotNull ASTNode node){
		super(node);
	}
	
	public JvmType asType(){
		if(getNode().findChildByType(Tokens.SQ_BRACES) != null)
			return PsiUtils.childOfType(this, KScrTypeRef.class)
					.map(KScrTypeRef::asType)
					.map(ArrayTypeImpl::of)
					.orElse(null);
		return PsiUtils.childOfType(this, KScrRawTypeRef.class)
				.map(KScrRawTypeRef::type).orElse(null);
	}
	
	public JvmClass asClass(){
		return JvmClassUtils.asClass(asType());
	}
}