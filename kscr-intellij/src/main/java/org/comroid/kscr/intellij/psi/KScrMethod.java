package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.util.PsiTreeUtil;
import org.comroid.kscr.intellij.psi.utils.KScrModifiersHolder;
import org.comroid.kscr.intellij.psi.utils.KScrVarScope;
import org.comroid.kscr.intellij.psi.utils.KScrVariable;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.List;
import java.util.Optional;

public class KScrMethod extends KScrDefinition implements KScrModifiersHolder, KScrVarScope{
	
	public KScrMethod(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrType getType(){
		// parent is KScrMember, then KScrType
		return (KScrType)getParent().getParent();
	}
	
	public String fullyQualifiedName(){
		return getType().fullyQualifiedName() + "::" + getName();
	}
	
	public KScrType containingType(){
		return PsiTreeUtil.getParentOfType(this, KScrType.class);
	}
	
	public Optional<KScrModifierList> modifiers(){
		return PsiUtils.childOfType(this, KScrModifierList.class);
	}
	
	public boolean isStatic(){
		return modifiers().map(x -> x.hasModifier("static")).orElse(false);
	}
	
	public Optional<KScrTypeRef> returns(){
		return PsiUtils.childOfType(this, KScrTypeRef.class);
	}
	
	public List<KScrParameter> parameters(){
		var paramList = PsiUtils.childOfType(this, KScrParametersList.class);
		if(paramList.isPresent())
			return PsiUtils.childrenOfType(paramList.get(), KScrParameter.class);
		return List.of();
	}
	
	public @Nullable JvmType returnType(){
		return returns().map(KScrTypeRef::asType).orElse(null);
	}
	
	public List<? extends KScrVariable> available(){
		return parameters();
	}
}