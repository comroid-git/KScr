package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.utils.KScrModifiersHolder;
import org.jetbrains.annotations.NotNull;

public class KScrVariableAssignment extends KScrElement{
	
	public KScrVariableAssignment(@NotNull ASTNode node){
		super(node);
	}
}