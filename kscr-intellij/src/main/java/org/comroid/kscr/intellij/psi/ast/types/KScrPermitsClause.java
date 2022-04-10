package org.comroid.kscr.intellij.psi.ast.types;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.stubs.StubKScrClassList;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.NotNull;

public class KScrPermitsClause extends KScrClassList<KScrPermitsClause>{
	
	public KScrPermitsClause(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrPermitsClause(@NotNull StubKScrClassList<KScrPermitsClause> list){
		super(list, StubTypes.KSCR_PERMITS_LIST);
	}
}