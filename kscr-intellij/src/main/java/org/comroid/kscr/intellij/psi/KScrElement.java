package org.comroid.kscr.intellij.psi;

import com.intellij.psi.PsiElement;

import java.util.Optional;

public interface KScrElement extends PsiElement{

    default Optional<KScrFileWrapper> getContainer(){
        var file = getContainingFile();
        if(file instanceof KScrFile)
            return ((KScrFile)file).wrapper();
        return Optional.empty();
    }
}