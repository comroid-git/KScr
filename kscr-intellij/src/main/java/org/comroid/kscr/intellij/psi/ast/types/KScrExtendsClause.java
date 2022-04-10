package org.comroid.kscr.intellij.psi.ast.types;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.stubs.StubKScrClassList;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.jetbrains.annotations.NotNull;

public class KScrExtendsClause extends KScrClassList<KScrExtendsClause>{
	
	public KScrExtendsClause(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrExtendsClause(@NotNull StubKScrClassList<KScrExtendsClause> list){
		super(list, StubTypes.KSCR_EXTENDS_LIST);
	}
}