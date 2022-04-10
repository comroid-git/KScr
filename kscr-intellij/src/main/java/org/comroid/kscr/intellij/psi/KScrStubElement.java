package org.comroid.kscr.intellij.psi;

import com.intellij.extapi.psi.StubBasedPsiElementBase;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.StubBasedPsiElement;
import com.intellij.psi.stubs.IStubElementType;
import com.intellij.psi.stubs.StubElement;
import org.jetbrains.annotations.NotNull;

public class KScrStubElement<Psi extends PsiElement, Stub extends StubElement<Psi>>
        extends StubBasedPsiElementBase<Stub>
        implements KScrElement, StubBasedPsiElement<Stub>{

    private PsiElement navigationElement = null;

    public KScrStubElement(@NotNull Stub stub, @NotNull IStubElementType<?, ?> nodeType){
        super(stub, nodeType);
    }

    public KScrStubElement(@NotNull ASTNode node){
        super(node);
    }

    public KScrStubElement<Psi, Stub> setNavigationElement(PsiElement navigationElement){
        this.navigationElement = navigationElement;
        return this;
    }

    public PsiElement getNavigationElement(){
        return navigationElement != null ? navigationElement : this;
    }
}