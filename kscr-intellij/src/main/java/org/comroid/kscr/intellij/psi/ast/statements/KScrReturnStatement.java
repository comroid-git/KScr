package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.ast.expressions.KScrExpression;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Optional;

public class KScrReturnStatement extends KScrAstElement implements KScrStatement{
	
	public KScrReturnStatement(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrExpression> returns(){
		return PsiUtils.childOfType(this, KScrExpression.class);
	}
	
	@Nullable("Null means no expression")
	public JvmType returnType(){
		return returns().map(KScrExpression::type).orElse(null);
	}
}