package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

public class KScrModifierList extends KScrElement{
	
	public KScrModifierList(@NotNull ASTNode node){
		super(node);
	}
	
	public boolean hasModifier(String modifier){
		return PsiUtils.childrenOfType(this, KScrModifier.class).stream().anyMatch(x -> x.textMatches(modifier));
	}
}