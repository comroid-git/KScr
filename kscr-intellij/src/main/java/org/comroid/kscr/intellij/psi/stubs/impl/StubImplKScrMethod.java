package org.comroid.kscr.intellij.psi.stubs.impl;

import com.intellij.psi.stubs.StubBase;
import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.KScrMethod;
import org.comroid.kscr.intellij.psi.stubs.StubKScrMethod;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class StubImplKScrMethod extends StubBase<KScrMethod> implements StubKScrMethod{
	
	@NotNull
	private final String name, returnType;
	private final boolean hasSemicolon;
	
	public StubImplKScrMethod(@Nullable StubElement parent, String name, String returnType, boolean semicolon){
		super(parent, StubTypes.KScr_METHOD);
		this.name = name;
		this.returnType = returnType;
		hasSemicolon = semicolon;
	}
	
	public @NotNull String name(){
		return name;
	}
	
	public @NotNull String returnTypeText(){
		return returnType;
	}
	
	public boolean hasSemicolon(){
		return hasSemicolon;
	}
}
