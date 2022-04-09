package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiReference;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.KScrIdHolder;
import org.comroid.kscr.intellij.psi.utils.KScrTypeReference;
import org.jetbrains.annotations.NotNull;

public class KScrAnnotation extends KScrAstElement implements KScrIdHolder{
	
	public KScrAnnotation(@NotNull ASTNode node){
		super(node);
	}
	
	public PsiReference getReference(){
		return getIdElement().map(id -> new KScrTypeReference(id, this)).orElse(null);
	}
}