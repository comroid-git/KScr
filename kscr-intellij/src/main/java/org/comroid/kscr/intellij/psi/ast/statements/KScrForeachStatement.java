package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.lang.jvm.JvmClassKind;
import com.intellij.lang.jvm.types.JvmArrayType;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.search.LocalSearchScope;
import com.intellij.psi.search.SearchScope;
import com.intellij.util.PlatformIcons;
import org.comroid.kscr.intellij.antlr_generated.KScrlicLangLexer;
import org.comroid.kscr.intellij.psi.KScrDefinitionAstElement;
import org.comroid.kscr.intellij.psi.KScrVariable;
import org.comroid.kscr.intellij.psi.Tokens;
import org.comroid.kscr.intellij.psi.ast.KScrTypeRef;
import org.comroid.kscr.intellij.psi.ast.KScrTypeRefOrInferred;
import org.comroid.kscr.intellij.psi.ast.expressions.KScrExpression;
import org.comroid.kscr.intellij.psi.ast.expressions.KScrIdExpr;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;
import java.util.Optional;

import static org.comroid.kscr.intellij.psi.utils.JvmClassUtils.typeByName;

// Introduces the for-each variable into scope
// Picked up by KScrBlock
public class KScrForeachStatement extends KScrDefinitionAstElement implements KScrVariable, KScrStatement{
	
	public KScrForeachStatement(@NotNull ASTNode node){
		super(node);
	}
	
	public String varName(){
		return getName();
	}
	
	public JvmType varType(){
		// TODO: once the compiler supports Iterables that aren't Objects, update to match
		return PsiUtils.childOfType(this, KScrTypeRefOrInferred.class)
				.flatMap(KScrTypeRefOrInferred::ref)
				.map(KScrTypeRef::asType)
				// for var/val
				.orElseGet(() -> {
					Optional<KScrExpression> expression = PsiUtils.childOfType(this, KScrExpression.class);
					if(expression.isPresent()){
						KScrExpression expr = expression.get();
						if(expr instanceof KScrIdExpr){
							var target = ((KScrIdExpr)expr).resolveTarget();
							if(target instanceof JvmClass && ((JvmClass)target).getClassKind() == JvmClassKind.ENUM)
								return expr.type();
						}
						var baseType = expr.type();
						if(baseType instanceof JvmArrayType)
							return ((JvmArrayType)baseType).getComponentType();
					}
					return typeByName("java.lang.Object", getProject());
				});
	}
	
	public boolean hasModifier(String modifier){
		if(!modifier.equals("final"))
			return false;
		return getNode().findChildByType(Tokens.getFor(KScrlicLangLexer.FINAL)) != null;
	}
	
	public boolean isLocal(){
		return true;
	}
	
	public @NotNull SearchScope getUseScope(){
		return new LocalSearchScope(getContainingFile());
	}
	
	public Optional<KScrExpression> iterator(){
		return PsiUtils.childOfType(this, KScrExpression.class);
	}
	
	public Optional<KScrStatement> body(){
		return PsiUtils.childOfType(this, KScrStatement.class);
	}
	
	public @Nullable Icon getIcon(int flags){
		return PlatformIcons.VARIABLE_ICON;
	}
}