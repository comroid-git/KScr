package org.comroid.kscr.intellij.psi.stubs.impl;

import com.intellij.psi.stubs.StubBase;
import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.common.KScrVariableDef;
import org.comroid.kscr.intellij.psi.stubs.StubKScrField;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class StubImplKScrField extends StubBase<KScrVariableDef> implements StubKScrField{
	
	@NotNull
	private final String name, typeText;
	
	public StubImplKScrField(@Nullable StubElement parent, @NotNull String name, @NotNull String typeText){
		super(parent, StubTypes.KScr_FIELD);
		this.name = name;
		this.typeText = typeText;
	}
	
	public @NotNull String name(){
		return name;
	}
	
	public @NotNull String typeText(){
		return typeText;
	}
}
