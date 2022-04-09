package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiType;
import com.intellij.psi.search.LocalSearchScope;
import com.intellij.psi.search.SearchScope;
import com.intellij.util.PlatformIcons;
import org.comroid.kscr.intellij.psi.KScrDefinitionAstElement;
import org.comroid.kscr.intellij.psi.KScrVarScope;
import org.comroid.kscr.intellij.psi.KScrVariable;
import org.comroid.kscr.intellij.psi.ast.KScrTypeRef;
import org.comroid.kscr.intellij.psi.ast.common.KScrBlock;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

// Introduces the catch variable into scope
// Not picked up by KScrBlock - not a KScrStatement, part of KScrTryCatchStatement
public class KScrCatchBlock extends KScrDefinitionAstElement implements KScrVariable, KScrVarScope{
	
	public KScrCatchBlock(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrStatement> body(){
		return PsiUtils.childOfType(this, KScrBlock.class).map(x -> x);
	}
	
	public String varName(){
		return getName();
	}
	
	public JvmType varType(){
		return PsiUtils.childOfType(this, KScrTypeRef.class)
				.map(KScrTypeRef::asType)
				.orElse(PsiType.NULL);
	}
	
	public boolean hasModifier(String modifier){
		return false;
	}
	
	public boolean isLocal(){
		return true;
	}
	
	public List<? extends KScrVariable> available(){
		List<KScrVariable> superScope = new ArrayList<>(KScrVarScope.scopeOf(this).map(KScrVarScope::available).orElse(List.of()));
		superScope.add(this);
		return superScope;
	}
	
	public @Nullable Icon getIcon(int flags){
		return PlatformIcons.VARIABLE_ICON;
	}
	
	public @NotNull SearchScope getUseScope(){
		return new LocalSearchScope(getContainingFile());
	}
}