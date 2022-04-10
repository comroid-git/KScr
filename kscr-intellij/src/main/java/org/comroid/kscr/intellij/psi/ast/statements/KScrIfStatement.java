package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.Optional;

public class KScrIfStatement extends KScrAstElement implements KScrStatement{
	
	public KScrIfStatement(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrStatement> body(){
		return PsiUtils.childOfType(this, KScrStatementWrapper.class)
				.flatMap(KScrStatementWrapper::inner);
	}
	
	public Optional<KScrElseClause> elseClause(){
		return PsiUtils.childOfType(this, KScrElseClause.class);
	}
	
	public Optional<KScrStatement> elseBody(){
		return elseClause().flatMap(KScrElseClause::body);
	}
}