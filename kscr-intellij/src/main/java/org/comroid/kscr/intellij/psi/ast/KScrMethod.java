package org.comroid.kscr.intellij.psi.ast;

import com.intellij.icons.AllIcons;
import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmMethod;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiPrimitiveType;
import com.intellij.psi.search.LocalSearchScope;
import com.intellij.psi.search.SearchScope;
import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.antlr_generated.KScrlicLangLexer;
import org.comroid.kscr.intellij.psi.KScrCodeHolder;
import org.comroid.kscr.intellij.psi.KScrDefinitionStubElement;
import org.comroid.kscr.intellij.psi.Tokens;
import org.comroid.kscr.intellij.psi.ast.common.KScrBlock;
import org.comroid.kscr.intellij.psi.ast.common.KScrParameter;
import org.comroid.kscr.intellij.psi.ast.statements.KScrStatement;
import org.comroid.kscr.intellij.psi.ast.statements.KScrStatementWrapper;
import org.comroid.kscr.intellij.psi.ast.types.KScrType;
import org.comroid.kscr.intellij.psi.stubs.StubKScrMethod;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.comroid.kscr.intellij.psi.types.KScrKind;
import org.comroid.kscr.intellij.psi.types.JvmKScrlicClass;
import org.comroid.kscr.intellij.psi.types.JvmKScrlicMethod;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;
import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;

public class KScrMethod extends KScrDefinitionStubElement<KScrMethod, StubKScrMethod> implements KScrCodeHolder{
	
	public KScrMethod(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrMethod(@NotNull StubKScrMethod method){
		super(method, StubTypes.KSCR_METHOD);
	}
	
	public String fullyQualifiedName(){
		return containingType().fullyQualifiedName() + "::" + getName();
	}
	
	public KScrType containingType(){
		return getStubOrPsiParentOfType(KScrType.class);
	}
	
	public Optional<KScrModifierList> modifiers(){
		var stub = getStub();
		if(stub != null)
			return Optional.ofNullable(stub.modifiers()).map(StubElement::getPsi);
		return PsiUtils.childOfType(this, KScrModifierList.class);
	}
	
	public boolean isStatic(){
		return modifiers().map(x -> x.hasModifier("static")).orElse(false);
	}
	
	public Optional<KScrTypeRef> returns(){
		var stub = getStub();
		if(stub != null){
			var type = PsiUtils.createTypeReferenceFromText(this, stub.returnTypeText());
			return Optional.of((KScrTypeRef)type);
		}
		return PsiUtils.childOfType(this, KScrTypeRef.class);
	}
	
	public List<KScrParameter> parameters(){
		var stub = getStub();
		if(stub != null){
			var params = stub.parameters();
			return params.stream().map(StubElement::getPsi).collect(Collectors.toList());
		}
		var paramList = PsiUtils.childOfType(this, KScrParametersList.class);
		return paramList.map(list -> PsiUtils.childrenOfType(list, KScrParameter.class)).orElseGet(List::of);
	}
	
	public @Nullable JvmType returnType(){
		return returns().map(KScrTypeRef::asType).orElse(PsiPrimitiveType.NULL);
	}
	
	public boolean overrides(JvmMethod other){
		return JvmClassUtils.overrides(JvmKScrlicMethod.of(this), other, getProject());
	}
	
	public @Nullable JvmMethod overriddenMethod(){
		return JvmClassUtils.findMethodInHierarchy(JvmKScrlicClass.of(containingType()), this::overrides, true);
	}
	
	public boolean hasSemicolon(){
		var stub = getStub();
		if(stub != null)
			return stub.hasSemicolon();
		
		var node = getNode().getLastChildNode().getFirstChildNode();
		return node != null && node.getElementType() == Tokens.getFor(KScrlicLangLexer.SEMICOLON);
	}
	
	public String getName(){
		var stub = getStub();
		if(stub != null)
			return stub.name();
		
		return super.getName();
	}
	
	public boolean hasModifier(String modifier){
		if(modifier.equals("abstract") && containingType().kind() == KScrKind.INTERFACE)
			if(hasSemicolon()) // note that a semicolon does not mean abstract in classes
				return true;
		return KScrCodeHolder.super.hasModifier(modifier);
	}
	
	public @Nullable Icon getIcon(int flags){
		// TODO: consider finality
		return hasModifier("abstract") ? AllIcons.Nodes.AbstractMethod : AllIcons.Nodes.Method;
	}
	
	public Optional<KScrStatement> body(){
		var body = getLastChild();
		if(body != null){
			if(body.getChildren().length > 1){
				// must be an arrow function
				return PsiUtils.childOfType(body, KScrStatementWrapper.class)
						.flatMap(KScrStatementWrapper::inner);
			}else
				return PsiUtils.childOfType(body, KScrBlock.class).map(KScrStatement.class::cast);
		}
		return Optional.empty();
	}
	
	public JvmMethod toJvm(){
		return JvmKScrlicMethod.of(this);
	}
	
	public @NotNull SearchScope getUseScope(){
		if(hasModifier("private"))
			return new LocalSearchScope(getContainingFile());
		return super.getUseScope();
	}
}