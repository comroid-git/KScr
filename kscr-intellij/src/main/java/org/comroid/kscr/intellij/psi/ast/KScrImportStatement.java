package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.psi.PsiReference;
import org.comroid.kscr.intellij.antlr_generated.KScrlicLangParser;
import org.comroid.kscr.intellij.psi.*;
import org.comroid.kscr.intellij.psi.utils.KScrTypeReference;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Collection;

public class KScrImportStatement extends KScrAstElement implements KScrIdHolder{
	
	public KScrImportStatement(@NotNull ASTNode node){
		super(node);
	}
	
	public boolean isStatic(){
		return !PsiUtils.matchingChildren(this, k -> k.getNode().getElementType() == Tokens.getFor(KScrlicLangParser.STATIC)).isEmpty();
	}
	
	public boolean isWildcard(){
		return !PsiUtils.matchingChildren(this, k -> k.getNode().getElementType() == Tokens.getFor(KScrlicLangParser.STAR)).isEmpty();
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
	
	@Nullable
	public static KScrImportStatement followToImport(JvmClass type, Collection<KScrImportStatement> list){
		if(type == null)
			return null;
		for(KScrImportStatement statement : list)
			if(statement.importsType(type))
				return statement;
		return null;
	}
	
	public static boolean isQualificationRedundant(KScrClassReference reference, Collection<KScrImportStatement> list){
		return reference.isQualified() && followToImport(reference.resolveClass(), list) != null;
	}
	
	// TODO: consider conflicting short names
	public static boolean isQualificationRedundant(KScrClassReference reference){
		KScrFile file = reference.containingKScrlicFile();
		if(file == null) // so there's no imports
			return false;
		return isQualificationRedundant(reference, file.getImports());
	}
}