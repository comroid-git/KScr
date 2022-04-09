package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import com.intellij.psi.stubs.EmptyStub;
import org.comroid.kscr.intellij.psi.KScrStubElement;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.NotNull;

public class KScrParametersList extends KScrStubElement<KScrParametersList, EmptyStub<KScrParametersList>>{
	
	public KScrParametersList(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrParametersList(@NotNull EmptyStub<KScrParametersList> stub){
		super(stub, StubTypes.KSCR_PARAMETERS_LIST);
	}
}