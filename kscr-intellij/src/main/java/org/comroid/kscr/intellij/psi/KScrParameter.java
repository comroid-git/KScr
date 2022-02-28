package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.search.LocalSearchScope;
import com.intellij.psi.search.SearchScope;
import com.intellij.util.PlatformIcons;
import org.comroid.kscr.intellij.antlr_generated.KScrLangLexer;
import org.comroid.kscr.intellij.psi.utils.KScrVariable;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;
import java.util.Optional;

public class KScrParameter extends KScrDefinition implements KScrVariable{
	
	public KScrParameter(@NotNull ASTNode node){
		super(node);
	}
	
	public String varName(){
		return getName();
	}
	
	public JvmType varType(){
		return getTypeName().map(KScrTypeRef::asType).orElse(null);
	}
	
	public boolean hasModifier(String modifier){
		if(!modifier.equals("final"))
			return false;
		return getNode().findChildByType(Tokens.getFor(KScrLangLexer.FINAL)) != null;
	}
	
	@NotNull
	public Optional<KScrTypeRef> getTypeName(){
		return PsiUtils.childOfType(this, KScrTypeRef.class);
	}
	
	public boolean isMethodParameter(){
		return !(getParent() instanceof KScrRecordComponents);
	}
	
	public @NotNull SearchScope getUseScope(){
		return isMethodParameter() ? new LocalSearchScope(getContainingFile()) : super.getUseScope();
	}
	
	public @Nullable Icon getIcon(int flags){
		if(isMethodParameter())
			return PlatformIcons.PARAMETER_ICON;
		else
			return PlatformIcons.FIELD_ICON;
	}
}