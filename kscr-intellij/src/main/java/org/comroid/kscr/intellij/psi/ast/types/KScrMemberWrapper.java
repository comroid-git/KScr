package org.comroid.kscr.intellij.psi.ast.types;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrStubElement;
import org.comroid.kscr.intellij.psi.stubs.StubKScrMemberWrapper;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.NotNull;

public class KScrMemberWrapper extends KScrStubElement<KScrMemberWrapper, StubKScrMemberWrapper>{
	
	public KScrMemberWrapper(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrMemberWrapper(@NotNull StubKScrMemberWrapper stub){
		super(stub, StubTypes.KSCR_MEMBER);
	}
}