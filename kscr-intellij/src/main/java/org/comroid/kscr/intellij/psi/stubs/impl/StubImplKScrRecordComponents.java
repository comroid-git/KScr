package org.comroid.kscr.intellij.psi.stubs.impl;

import com.intellij.psi.stubs.StubBase;
import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.types.KScrRecordComponents;
import org.comroid.kscr.intellij.psi.stubs.StubKScrRecordComponents;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.Nullable;

public class StubImplKScrRecordComponents extends StubBase<KScrRecordComponents> implements StubKScrRecordComponents{
	
	public StubImplKScrRecordComponents(@Nullable StubElement parent){
		super(parent, StubTypes.KScr_RECORD_COMPONENTS);
	}
}