package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import org.jetbrains.annotations.NotNull;

public class KScrBinaryOp extends KScrElement{
	
	public KScrBinaryOp(@NotNull ASTNode node){
		super(node);
	}
}