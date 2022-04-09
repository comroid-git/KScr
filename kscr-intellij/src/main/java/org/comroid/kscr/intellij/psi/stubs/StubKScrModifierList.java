package org.comroid.kscr.intellij.psi.stubs;

import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.KScrModifierList;
import org.jetbrains.annotations.NotNull;

import java.util.List;

public interface StubKScrModifierList extends StubElement<KScrModifierList>{
	
	@NotNull
	List<String> modifiers();
}