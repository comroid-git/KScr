package org.comroid.kscr.intellij.psi.stubs;

import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.types.KScrClassList;
import org.jetbrains.annotations.NotNull;

import java.util.List;

public interface StubKScrClassList<CL extends KScrClassList<CL>> extends StubElement<CL> {
    @NotNull
    List<String> elementFqNames();
}
