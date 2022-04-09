package org.comroid.kscr.intellij.psi.stubs.impl;

import com.intellij.psi.stubs.StubBase;
import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.common.KScrParameter;
import org.comroid.kscr.intellij.psi.stubs.StubKScrParameter;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class StubImplKScrParameter extends StubBase<KScrParameter> implements StubKScrParameter{
	
	@NotNull
	private final String name, typeText;
	private final boolean varargs;
	
	public StubImplKScrParameter(@Nullable StubElement parent,
	                            @NotNull String name,
	                            @NotNull String typeText,
	                            boolean varargs){
		super(parent, StubTypes.KScr_PARAMETER);
		this.name = name;
		this.typeText = typeText;
		this.varargs = varargs;
	}
	
	@NotNull
	public String name(){
		return name;
	}
	
	public @NotNull String typeText(){
		return typeText;
	}
	
	public boolean isVarargs(){
		return varargs;
	}
}
