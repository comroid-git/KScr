package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmArrayType;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.search.LocalSearchScope;
import com.intellij.psi.search.SearchScope;
import org.comroid.kscr.intellij.antlr_generated.KScrLangLexer;
import org.comroid.kscr.intellij.psi.utils.KScrVariable;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import static org.comroid.kscr.intellij.psi.utils.JvmClassUtils.getByName;

// Introduces the for-each variable into scope
public class KScrForeachLoop extends KScrDefinition implements KScrVariable{
	
	public KScrForeachLoop(@NotNull ASTNode node){
		super(node);
	}
	
	public String varName(){
		return getName();
	}
	
	public JvmType varType(){
		// TODO: once the compiler supports Iterables that aren't Objects, update to match
		return PsiUtils.childOfType(this, KScrTypeRef.class)
				.map(KScrTypeRef::asType)
				// for var/val
				.orElseGet(() -> {
					var baseType = PsiUtils.childOfType(this, KScrExpression.class).map(KScrExpression::type).orElse(null);
					return baseType instanceof JvmArrayType ? ((JvmArrayType)baseType).getComponentType() : getByName("java.lang.Object", getProject());
				});
	}
	
	public boolean hasModifier(String modifier){
		if(!modifier.equals("final"))
			return false;
		return getNode().findChildByType(Tokens.getFor(KScrLangLexer.FINAL)) != null;
	}
	
	public @NotNull SearchScope getUseScope(){
		return new LocalSearchScope(getContainingFile());
	}
}