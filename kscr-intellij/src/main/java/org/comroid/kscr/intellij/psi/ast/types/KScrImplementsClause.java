package org.comroid.kscr.intellij.psi.ast.types;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.stubs.StubKScrClassList;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.NotNull;

public class KScrImplementsClause extends KScrClassList<KScrImplementsClause>{
	
	public KScrImplementsClause(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrImplementsClause(@NotNull StubKScrClassList<KScrImplementsClause> list){
		super(list, StubTypes.KSCR_IMPLEMENTS_LIST);
	}
}