package org.comroid.kscr.intellij.psi;

import com.intellij.extapi.psi.ASTWrapperPsiElement;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.ResolveState;
import com.intellij.psi.scope.PsiScopeProcessor;
import org.comroid.kscr.intellij.antlr_generated.KScrParser;
import org.antlr.intellij.adaptor.lexer.RuleIElementType;
import org.antlr.intellij.adaptor.psi.Trees;
import org.comroid.kscr.intellij.psi.KScrElement;
import org.jetbrains.annotations.NotNull;

public class KScrAstElement extends ASTWrapperPsiElement implements KScrElement {

    public KScrAstElement(@NotNull ASTNode node){
        super(node);
    }

    public PsiElement @NotNull [] getChildren(){
        // include leaves
        return Trees.getChildren(this);
    }

    public String toString(){
        if(getClass() != KScrAstElement.class)
            return getClass().getSimpleName();
        boolean isRule = getNode().getElementType() instanceof RuleIElementType;
        if(isRule && KScrParser.ruleNames.length < ((RuleIElementType)getNode().getElementType()).getRuleIndex())
            return getClass().getSimpleName() + "(" + KScrParser.ruleNames[((RuleIElementType)getNode().getElementType()).getRuleIndex()] + ")";
        return getClass().getSimpleName();
    }

    public boolean processDeclarations(@NotNull PsiScopeProcessor processor, @NotNull ResolveState state, PsiElement lastParent, @NotNull PsiElement place){
        boolean cont = true;
        for(PsiElement child : getChildren())
            if(cont && lastParent != child)
                cont = processor.execute(child, state);
        return cont;
    }
}