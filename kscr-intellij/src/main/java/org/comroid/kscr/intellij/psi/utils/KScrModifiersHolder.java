package org.comroid.kscr.intellij.psi.utils;

import com.intellij.psi.PsiElement;

import java.util.Optional;

public interface KScrModifiersHolder extends PsiElement{
	
	default Optional<KScrModifierList> getModifiersElement(){
		return PsiUtils.childOfType(this, KScrModifierList.class);
	}
	
	default boolean hasModifier(String modifier){
		return getModifiersElement().map(k -> k.hasModifier(modifier)).orElse(false);
	}
}