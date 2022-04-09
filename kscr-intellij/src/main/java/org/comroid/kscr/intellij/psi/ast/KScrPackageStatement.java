package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Optional;

public class KScrPackageStatement extends KScrAstElement{
	
	public KScrPackageStatement(@NotNull ASTNode node){
		super(node);
	}
	
	@Nullable
	public String getPackageName(){
		return getId().map(PsiElement::getText).orElse(null);
	}
	
	public @NotNull Optional<KScrId> getId(){
		return PsiUtils.childOfType(this, KScrId.class);
	}
	
	// TODO: resolve against packages
}