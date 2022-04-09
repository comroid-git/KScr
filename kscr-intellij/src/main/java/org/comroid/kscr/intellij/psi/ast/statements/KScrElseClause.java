package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.Optional;

// not a KScrStatement, part of KScrIfStatement
public class KScrElseClause extends KScrAstElement{
	
	public KScrElseClause(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrStatement> body(){
		return PsiUtils.childOfType(this, KScrStatementWrapper.class)
				.flatMap(KScrStatementWrapper::inner);
	}
}