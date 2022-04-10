package org.comroid.kscr.intellij.psi.stubs.impl;

import com.intellij.psi.stubs.StubBase;
import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.types.KScrMemberWrapper;
import org.comroid.kscr.intellij.psi.stubs.StubKScrMemberWrapper;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.Nullable;

public class StubImplKScrMemberWrapper extends StubBase<KScrMemberWrapper> implements StubKScrMemberWrapper{
	
	public StubImplKScrMemberWrapper(@Nullable StubElement parent){
		super(parent, StubTypes.KScr_MEMBER);
	}
}