package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.search.LocalSearchScope;
import com.intellij.psi.search.SearchScope;
import com.intellij.psi.util.PsiTreeUtil;
import com.intellij.util.PlatformIcons;
import org.comroid.kscr.intellij.psi.utils.KScrModifiersHolder;
import org.comroid.kscr.intellij.psi.utils.KScrVariable;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;
import java.util.Optional;

public class KScrVariableDef extends KScrDefinition implements KScrVariable, KScrModifiersHolder{
	
	public KScrVariableDef(@NotNull ASTNode node){
		super(node);
	}
	
	public String varName(){
		return getName();
	}
	
	public JvmType varType(){
		return PsiUtils.childOfType(this, KScrTypeRef.class)
				.map(KScrTypeRef::asType)
				// for var/val
				.orElseGet(() -> PsiUtils.childOfType(this, KScrExpression.class).map(KScrExpression::type).orElse(null));
	}
	
	public boolean hasModifier(String modifier){
		return KScrModifiersHolder.super.hasModifier(modifier);
	}
	
	public boolean isLocalVar(){
		return PsiTreeUtil.getParentOfType(this, KScrStatement.class) != null;
	}
	
	public Optional<KScrExpression> initializer(){
		return PsiUtils.childOfType(this, KScrExpression.class);
	}
	
	public boolean hasInferredType(){
		return PsiUtils.childOfType(this, KScrTypeRef.class)
				.map(x -> x.getText().equals("var") || x.getText().equals("val"))
				.orElse(false);
	}
	
	public @NotNull SearchScope getUseScope(){
		return isLocalVar() ? new LocalSearchScope(getContainingFile()) : super.getUseScope();
	}
	
	public @Nullable Icon getIcon(int flags){
		if(isLocalVar())
			return PlatformIcons.VARIABLE_ICON;
		else
			return PlatformIcons.FIELD_ICON;
	}
}