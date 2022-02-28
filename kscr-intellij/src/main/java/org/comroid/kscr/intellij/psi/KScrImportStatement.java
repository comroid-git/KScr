package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.psi.PsiReference;
import org.comroid.kscr.intellij.antlr_generated.KScrLangParser;
import org.comroid.kscr.intellij.psi.utils.KScrIdHolder;
import org.comroid.kscr.intellij.psi.utils.KScrTypeReference;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

public class KScrImportStatement extends KScrElement implements KScrIdHolder{
	
	public KScrImportStatement(@NotNull ASTNode node){
		super(node);
	}
	
	public boolean isStatic(){
		return !PsiUtils.matchingChildren(this, k -> k.getNode().getElementType() == Tokens.getFor(KScrLangParser.STATIC)).isEmpty();
	}
	
	public boolean isWildcard(){
		return !PsiUtils.matchingChildren(this, k -> k.getNode().getElementType() == Tokens.getFor(KScrLangParser.STAR)).isEmpty();
	}
	
	public String getImportName(){
		return getIdElement().map(KScrId::getText).orElse("");
	}
	
	public PsiReference getReference(){
		if(isWildcard())
			return null;
		return getIdElement().map(id -> new KScrTypeReference(id, this)).orElse(null);
	}
	
	public boolean importsType(JvmClass cpc){
		return importsType(cpc.getQualifiedName());
	}
	
	public boolean importsType(String fqTypeName){
		if(isStatic())
			return false;
		return importsType(getImportName() + (isWildcard() ? ".*" : ""), fqTypeName);
	}
	
	public static boolean importsType(String importName, String fqTypeName){
		if(importName.endsWith(".*")){
			String baseName = importName.substring(0, importName.length() - 1);
			return fqTypeName.startsWith(baseName) && !fqTypeName.substring(baseName.length()).contains(".");
		}
		return fqTypeName.equals(importName);
	}
}
