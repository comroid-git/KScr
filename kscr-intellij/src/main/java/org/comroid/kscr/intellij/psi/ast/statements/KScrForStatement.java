package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.KScrVarScope;
import org.comroid.kscr.intellij.psi.KScrVariable;
import org.comroid.kscr.intellij.psi.ast.common.KScrVariableDef;
import org.comroid.kscr.intellij.psi.ast.expressions.KScrExpression;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

public class KScrForStatement extends KScrAstElement implements KScrStatement, KScrVarScope{
	
	public KScrForStatement(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrStatementWrapper> start(){
		return PsiUtils.childOfType(this, KScrStatementWrapper.class, 0);
	}
	
	public Optional<KScrExpression> condition(){
		return PsiUtils.childOfType(this, KScrExpression.class, 0);
	}
	
	public Optional<KScrStatementWrapper> updater(){
		return PsiUtils.childOfType(this, KScrStatementWrapper.class, 1);
	}
	
	public Optional<KScrStatementWrapper> body(){
		return PsiUtils.childOfType(this, KScrStatementWrapper.class, 2);
	}
	
	public List<? extends KScrVariable> available(){
		List<KScrVariable> superScope = new ArrayList<>(KScrVarScope.scopeOf(this).map(KScrVarScope::available).orElse(List.of()));
		// add the index variable (or whatever)
		start().flatMap(KScrStatementWrapper::inner).ifPresent(s -> {
			if(s instanceof KScrVariableDef)
				superScope.add((KScrVariable)s);
		});
		return superScope;
	}
}