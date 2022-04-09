package org.comroid.kscr.intellij.psi.ast.common;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.KScrVarScope;
import org.comroid.kscr.intellij.psi.KScrVariable;
import org.comroid.kscr.intellij.psi.ast.statements.KScrStatement;
import org.comroid.kscr.intellij.psi.ast.statements.KScrStatementWrapper;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class KScrBlock extends KScrAstElement implements KScrVarScope, KScrStatement{
	
	public KScrBlock(@NotNull ASTNode node){
		super(node);
	}
	
	public List<? extends KScrVariable> available(){
		// all wrapped KScrVariableDefs
		// plus our super-scope's variable
		var defined = PsiUtils.wrappedChildrenOfType(this, KScrVariable.class);
		var available = new ArrayList<>(defined);
		KScrVarScope.scopeOf(this).ifPresent(scope -> available.addAll(scope.available()));
		return available;
	}
	
	public Stream<KScrStatement> streamBody(){
		return PsiUtils.streamChildrenOfType(this, KScrStatementWrapper.class)
				.flatMap(x -> Stream.ofNullable(x.inner().orElse(null)));
	}
	
	public List<KScrStatement> getBody(){
		return streamBody().collect(Collectors.toList());
	}
}