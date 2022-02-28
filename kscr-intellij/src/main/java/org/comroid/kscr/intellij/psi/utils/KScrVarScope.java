package org.comroid.kscr.intellij.psi.utils;

import com.intellij.psi.PsiElement;
import com.intellij.psi.util.PsiTreeUtil;

import java.util.List;
import java.util.Optional;

public interface KScrVarScope extends PsiElement{
	
	List<? extends KScrVariable> available();
	
	static Optional<KScrVarScope> scopeOf(PsiElement e){
		return Optional.ofNullable(PsiTreeUtil.getParentOfType(e, KScrVarScope.class));
	}
}