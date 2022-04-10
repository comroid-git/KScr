package org.comroid.kscr.intellij.psi.stubs;

import org.comroid.kscr.intellij.psi.ast.common.KScrParameter;
import org.jetbrains.annotations.NotNull;

public interface StubKScrParameter extends StubAsKScrVariable<KScrParameter>{
	
	@NotNull
	String name();
	
	@NotNull
	String typeText();
	
	boolean isVarargs();
	
	@NotNull
	default String varName(){
		return name();
	}
	
	@NotNull
	default String varTypeText(){
		return typeText();
	}
	
	default boolean hasModifier(String modifier){
		if(!(modifier.equals("private") || modifier.equals("final")))
			return false;
		// method parameter finality is not externally visible
		return isRecordComponent();
	}
	
	default boolean isRecordComponent(){
		return getParentStub() instanceof StubKScrRecordComponents;
	}
}