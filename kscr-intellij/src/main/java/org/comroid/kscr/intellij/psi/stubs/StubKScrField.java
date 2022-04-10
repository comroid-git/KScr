package org.comroid.kscr.intellij.psi.stubs;

import org.comroid.kscr.intellij.psi.ast.common.KScrVariableDef;
import org.jetbrains.annotations.NotNull;

public interface StubKScrField extends StubWithKScrModifiers<KScrVariableDef>, StubAsKScrVariable<KScrVariableDef>{
	
	@NotNull
	String name();
	
	@NotNull
	String typeText();
	
	@NotNull
	default String varName(){
		return name();
	}
	
	@NotNull
	default String varTypeText(){
		return typeText();
	}
	
	default boolean hasModifier(String modifier){
		var modList = modifiers();
		return modList != null && modList.modifiers().contains(modifier);
	}
}