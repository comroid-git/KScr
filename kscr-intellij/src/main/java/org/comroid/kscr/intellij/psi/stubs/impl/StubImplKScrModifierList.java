package org.comroid.kscr.intellij.psi.stubs.impl;

import com.intellij.psi.stubs.StubBase;
import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.KScrModifierList;
import org.comroid.kscr.intellij.psi.stubs.StubKScrModifierList;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.List;

public class StubImplKScrModifierList extends StubBase<KScrModifierList> implements StubKScrModifierList{
	
	@NotNull
	private final List<String> modifiers;
	
	public StubImplKScrModifierList(@Nullable StubElement parent, @NotNull List<String> modifiers){
		super(parent, StubTypes.KScr_MODIFIER_LIST);
		this.modifiers = modifiers;
	}
	
	public @NotNull List<String> modifiers(){
		return modifiers;
	}
}