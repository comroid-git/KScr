package org.comroid.kscr.intellij.psi.stubs;

import com.intellij.psi.PsiElement;
import com.intellij.psi.stubs.StubElement;
import org.jetbrains.annotations.Nullable;

public interface StubWithKScrModifiers<Psi extends PsiElement> extends StubElement<Psi>{
	
	@Nullable
	default StubKScrModifierList modifiers(){
		return findChildStubByType(StubTypes.KScr_MODIFIER_LIST);
	}
}