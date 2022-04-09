package org.comroid.kscr.intellij.psi.stubs;

import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.types.KScrExtendsClause;
import org.comroid.kscr.intellij.psi.ast.types.KScrImplementsClause;
import org.comroid.kscr.intellij.psi.ast.types.KScrPermitsClause;
import org.comroid.kscr.intellij.psi.ast.types.KScrType;
import org.comroid.kscr.intellij.psi.types.KScrKind;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public interface StubCycType extends StubElement<KScrType> {

    @NotNull
    String fullyQualifiedName();

    @NotNull
    String shortName();

    @NotNull
    KScrKind kind();

    @Nullable
    default StubKScrClassList<KScrExtendsClause> extendsList(){
        return findChildStubByType(StubTypes.CYC_EXTENDS_LIST);
    }

    @Nullable
    default StubKScrClassList<KScrImplementsClause> implementsList(){
        return findChildStubByType(StubTypes.CYC_IMPLEMENTS_LIST);
    }

    @Nullable
    default StubKScrClassList<KScrPermitsClause> permitsList(){
        return findChildStubByType(StubTypes.CYC_PERMITS_LIST);
    }
}
