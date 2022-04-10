package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.Optional;

public class KScrTypeRefOrInferred extends KScrAstElement{
	
	public KScrTypeRefOrInferred(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrTypeRef> ref(){
		return PsiUtils.childOfType(this, KScrTypeRef.class);
	}
}
