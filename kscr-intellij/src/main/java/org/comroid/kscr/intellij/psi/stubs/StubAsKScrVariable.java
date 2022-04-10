package org.comroid.kscr.intellij.psi.stubs;

import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.KScrVariable;
import org.jetbrains.annotations.NotNull;

public interface StubAsKScrVariable<Psi extends KScrVariable> extends StubElement<Psi>{
	
	@NotNull
	String varName();
	
	@NotNull
	String varTypeText();
	
	boolean hasModifier(String modifier);
}