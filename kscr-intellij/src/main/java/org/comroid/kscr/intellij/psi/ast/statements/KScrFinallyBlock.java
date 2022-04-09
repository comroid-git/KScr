package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.ast.common.KScrBlock;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.Optional;

// not a KScrStatement, part of KScrTryCatchStatement
public class KScrFinallyBlock extends KScrAstElement{
	
	public KScrFinallyBlock(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrStatement> body(){
		return PsiUtils.childOfType(this, KScrBlock.class).map(x -> x);
	}
}