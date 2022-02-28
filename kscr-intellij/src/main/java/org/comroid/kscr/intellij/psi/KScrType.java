package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiFile;
import com.intellij.util.IncorrectOperationException;
import com.intellij.util.PlatformIcons;
import org.comroid.kscr.intellij.KScrIcons;
import org.comroid.kscr.intellij.antlr_generated.KScrLangParser;
import org.comroid.kscr.intellij.psi.types.KScrKind;
import org.comroid.kscr.intellij.psi.utils.KScrModifiersHolder;
import org.comroid.kscr.intellij.psi.utils.KScrVariable;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;

public class KScrType extends KScrDefinition implements KScrModifiersHolder{
	
	public KScrType(@NotNull ASTNode node){
		super(node);
	}
	
	public boolean isTopLevelType(){
		return getParent() instanceof KScrFileWrapper;
	}
	
	public String getPackageName(){
		if(getContainingFile() instanceof KScrFile){
			KScrFile file = (KScrFile)getContainingFile();
			return file.getPackage().map(KScrPackageStatement::getPackageName).orElse("");
		}
		return "";
	}
	
	public @NotNull String fullyQualifiedName(){
		PsiFile file = getContainingFile();
		if(file instanceof KScrFile)
			return ((KScrFile)file).getPackage().map(k -> k.getPackageName() + ".").orElse("") + super.fullyQualifiedName();
		return super.fullyQualifiedName();
	}
	
	public @NotNull KScrKind kind(){
		var objType = getNode().findChildByType(Tokens.getRuleFor(KScrLangParser.RULE_objectType));
		if(objType != null){
			if(objType.findChildByType(Tokens.TOK_CLASS) != null)
				return KScrKind.CLASS;
			if(objType.findChildByType(Tokens.TOK_INTERFACE) != null)
				return KScrKind.INTERFACE;
			if(objType.findChildByType(Tokens.TOK_ANNOTATION) != null || objType.findChildByType(Tokens.TOK_AT) != null)
				return KScrKind.ANNOTATION;
			if(objType.findChildByType(Tokens.TOK_ENUM) != null)
				return KScrKind.ENUM;
			if(objType.findChildByType(Tokens.TOK_RECORD) != null)
				return KScrKind.RECORD;
			if(objType.findChildByType(Tokens.TOK_SINGLE) != null)
				return KScrKind.SINGLE;
		}
		return KScrKind.CLASS;
	}
	
	public boolean isFinal(){
		return hasModifier("final");
	}
	
	public @NotNull List<? extends KScrMethod> methods(){
		return PsiUtils.wrappedChildrenOfType(this, KScrMethod.class);
	}
	
	public @NotNull List<? extends KScrVariable> fields(){
		List<KScrVariable> defs = new ArrayList<>(PsiUtils.wrappedChildrenOfType(this, KScrVariableDef.class));
		PsiUtils.childOfType(this, KScrRecordComponents.class).ifPresent(rc -> defs.addAll(rc.components()));
		return defs;
	}
	
	public PsiElement setName(@NotNull String name) throws IncorrectOperationException{
		// also change the file name if top level
		if(isTopLevelType())
			getContainingFile().setName(name + ".KScr");
		return super.setName(name);
	}
	
	public List<KScrMember> getMembers(){
		return PsiUtils.childrenOfType(this, KScrMember.class);
	}
	
	public @NotNull String name(){
		return getName();
	}
	
	public @Nullable Icon getIcon(int flags){
		var objType = getNode().findChildByType(Tokens.getRuleFor(KScrLangParser.RULE_objectType));
		if(objType != null){
			if(objType.findChildByType(Tokens.TOK_CLASS) != null)
				return PlatformIcons.CLASS_ICON;
			if(objType.findChildByType(Tokens.TOK_INTERFACE) != null)
				return PlatformIcons.INTERFACE_ICON;
			if(objType.findChildByType(Tokens.TOK_ANNOTATION) != null || objType.findChildByType(Tokens.TOK_AT) != null)
				return PlatformIcons.ANNOTATION_TYPE_ICON;
			if(objType.findChildByType(Tokens.TOK_ENUM) != null)
				return PlatformIcons.ENUM_ICON;
			if(objType.findChildByType(Tokens.TOK_RECORD) != null)
				return PlatformIcons.RECORD_ICON;
			if(objType.findChildByType(Tokens.TOK_SINGLE) != null)
				return KScrIcons.SINGLE;
		}
		return KScrIcons.KScr_ICON;
	}
	
	@Nullable
	public JvmClass getSuperType(){
		if(kind() == KScrKind.INTERFACE)
			return null;
		var exts = PsiUtils.childOfType(this, KScrExtendsClause.class);
		return exts.flatMap(clause -> PsiUtils.childOfType(clause, KScrTypeRef.class).map(KScrTypeRef::asClass)).orElse(null);
	}
	
	@NotNull
	public List<JvmClass> getInterfaces(){
		Optional<? extends KScrElement> list;
		if(kind() == KScrKind.INTERFACE)
			list = PsiUtils.childOfType(this, KScrExtendsClause.class);
		else
			list = PsiUtils.childOfType(this, KScrImplementsClause.class);
		return list.map(x -> PsiUtils.childrenOfType(x, KScrTypeRef.class)
				.stream().map(KScrTypeRef::asClass).collect(Collectors.toList())).orElse(List.of());
	}
}