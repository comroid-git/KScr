package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.Optional;

public class KScrStatementWrapper extends KScrAstElement{
	
	public KScrStatementWrapper(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrStatement> inner(){
		return PsiUtils.childOfType(this, KScrStatement.class);
	}
}