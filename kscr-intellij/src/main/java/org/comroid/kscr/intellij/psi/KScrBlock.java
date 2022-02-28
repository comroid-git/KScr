package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.utils.KScrVarScope;
import org.comroid.kscr.intellij.psi.utils.KScrVariable;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.ArrayList;
import java.util.List;

public class KScrBlock extends KScrElement implements KScrVarScope{
	
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
}