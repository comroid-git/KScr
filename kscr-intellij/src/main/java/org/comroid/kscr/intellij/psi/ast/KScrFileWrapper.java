package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import com.intellij.psi.util.PsiTreeUtil;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.ast.types.KScrType;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

public class KScrFileWrapper extends KScrAstElement {
	
	public KScrFileWrapper(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrPackageStatement> getPackage(){
		return PsiUtils.childOfType(this, KScrPackageStatement.class);
	}
	
	public List<KScrImportStatement> getImports(){
		return new ArrayList<>(PsiTreeUtil.findChildrenOfType(this, KScrImportStatement.class));
	}
	
	public Optional<KScrType> getTypeDef(){
		return PsiUtils.childOfType(this, KScrType.class);
	}
}