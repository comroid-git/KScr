package org.comroid.kscr.intellij.psi;

import com.intellij.extapi.psi.ASTWrapperPsiElement;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.ResolveState;
import com.intellij.psi.scope.PsiScopeProcessor;
import org.comroid.kscr.intellij.antlr_generated.KScrLangParser;
import org.antlr.intellij.adaptor.lexer.RuleIElementType;
import org.antlr.intellij.adaptor.psi.Trees;
import org.jetbrains.annotations.NotNull;

import java.util.Optional;

public class KScrElement extends ASTWrapperPsiElement{
	
	public KScrElement(@NotNull ASTNode node){
		super(node);
	}
	
	public PsiElement @NotNull [] getChildren(){
		return Trees.getChildren(this);
	}
	
	public String toString(){
		boolean isRule = getNode().getElementType() instanceof RuleIElementType;
		if(isRule && KScrLangParser.ruleNames.length < ((RuleIElementType)getNode().getElementType()).getRuleIndex())
			return getClass().getSimpleName() + "(" + KScrLangParser.ruleNames[((RuleIElementType)getNode().getElementType()).getRuleIndex()] + ")";
		return getClass().getSimpleName();
	}
	
	public boolean processDeclarations(@NotNull PsiScopeProcessor processor, @NotNull ResolveState state, PsiElement lastParent, @NotNull PsiElement place){
		boolean cont = true;
		for(PsiElement child : getChildren())
			if(cont && lastParent != child)
				cont = processor.execute(child, state);
		return cont;
	}
	
	public boolean textMatches(String text){
		return getText().equals(text);
	}
	
	public Optional<KScrFileWrapper> getContainer(){
		var file = getContainingFile();
		if(file instanceof KScrFile)
			return ((KScrFile)file).wrapper();
		return Optional.empty();
	}
}