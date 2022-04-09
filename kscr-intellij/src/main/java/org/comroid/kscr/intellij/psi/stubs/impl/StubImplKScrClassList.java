package org.comroid.kscr.intellij.psi.stubs.impl;

import com.intellij.psi.stubs.IStubElementType;
import com.intellij.psi.stubs.StubBase;
import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.types.KScrClassList;
import org.comroid.kscr.intellij.psi.stubs.StubKScrClassList;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.List;

public class StubImplKScrClassList<CL extends KScrClassList<CL>> extends StubBase<CL> implements StubKScrClassList<CL>{
	
	@NotNull
	private final List<String> elementFqNames;
	
	public StubImplKScrClassList(@Nullable StubElement parent, IStubElementType elementType, @NotNull List<String> fqNames){
		super(parent, elementType);
		elementFqNames = fqNames;
	}
	
	public @NotNull List<String> elementFqNames(){
		return elementFqNames;
	}
}