package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import org.comroid.kscr.intellij.psi.KScrStubElement;
import org.comroid.kscr.intellij.psi.stubs.StubKScrModifierList;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.List;
import java.util.stream.Collectors;

public class KScrModifierList extends KScrStubElement<KScrModifierList, StubKScrModifierList>{
	
	public KScrModifierList(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrModifierList(@NotNull StubKScrModifierList list){
		super(list, StubTypes.KSCR_MODIFIER_LIST);
	}
	
	public boolean hasModifier(String modifier){
		if(getStub() != null)
			return getStub().modifiers().contains(modifier);
		
		return PsiUtils.streamChildrenOfType(this, KScrModifier.class).anyMatch(x -> x.textMatches(modifier));
	}
	
	public List<String> getModifiers(){
		if(getStub() != null)
			return getStub().modifiers();
		
		return PsiUtils.streamChildrenOfType(this, KScrModifier.class).map(PsiElement::getText).collect(Collectors.toList());
	}
}