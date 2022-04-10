package org.comroid.kscr.intellij.psi.ast.common;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiPrimitiveType;
import com.intellij.psi.search.LocalSearchScope;
import com.intellij.psi.search.SearchScope;
import com.intellij.psi.util.PsiTreeUtil;
import com.intellij.util.PlatformIcons;
import org.comroid.kscr.intellij.psi.KScrDefinitionStubElement;
import org.comroid.kscr.intellij.psi.KScrModifiersHolder;
import org.comroid.kscr.intellij.psi.KScrVariable;
import org.comroid.kscr.intellij.psi.ast.KScrTypeRef;
import org.comroid.kscr.intellij.psi.ast.KScrTypeRefOrInferred;
import org.comroid.kscr.intellij.psi.ast.expressions.KScrExpression;
import org.comroid.kscr.intellij.psi.ast.statements.KScrStatement;
import org.comroid.kscr.intellij.psi.ast.statements.KScrStatementWrapper;
import org.comroid.kscr.intellij.psi.ast.types.KScrType;
import org.comroid.kscr.intellij.psi.stubs.StubKScrField;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.comroid.kscr.intellij.psi.types.ClassTypeImpl;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;
import java.util.Optional;

public class KScrVariableDef extends KScrDefinitionStubElement<KScrVariableDef, StubKScrField>
		implements KScrVariable, KScrModifiersHolder, KScrStatement{
	
	public KScrVariableDef(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrVariableDef(@NotNull StubKScrField field){
		super(field, StubTypes.KSCR_FIELD);
	}
	
	public String varName(){
		return getName();
	}
	
	public JvmType varType(){
		var stub = getStub();
		if(stub != null){
			// val is allowed here for enum fields only
			String text = stub.typeText();
			KScrType container = stub.getParentStubOfType(KScrType.class);
			if(text.equals("val") && container != null)
				return ClassTypeImpl.of(container);
			var type = PsiUtils.createTypeReferenceFromText(this, text);
			return Optional.of((KScrTypeRef)type)
					.map(KScrTypeRef::asType)
					.orElse(PsiPrimitiveType.NULL);
		}
		
		return PsiUtils.childOfType(this, KScrTypeRefOrInferred.class)
				.flatMap(KScrTypeRefOrInferred::ref)
				.map(KScrTypeRef::asType)
				// for var/val
				.orElseGet(() -> {
					if(!isLocal()){
						if(PsiUtils.childOfType(this, KScrTypeRefOrInferred.class).map(PsiElement::getText).orElse("").equals("val"))
							return ClassTypeImpl.of(PsiTreeUtil.getParentOfType(this, KScrType.class));
						return PsiPrimitiveType.NULL;
					}
					return PsiUtils.childOfType(this, KScrExpression.class)
							.map(KScrExpression::type)
							.orElse(PsiPrimitiveType.NULL);
				});
	}
	
	public boolean hasModifier(String modifier){
		if(modifier.equals("final") || modifier.equals("static") || modifier.equals("public")){
			var stub = getStub(); // only fields have stubs
			if(stub != null){
				if(stub.typeText().equals("val"))
					return true;
			}else{
				var type = PsiUtils.childOfType(this, KScrTypeRefOrInferred.class);
				if(!isLocal() && type.isPresent() && type.get().getText().equals("val"))
					return true;
			}
		}
		return KScrModifiersHolder.super.hasModifier(modifier);
	}
	
	public boolean isLocal(){
		if(getStub() != null)
			return false;
		return PsiTreeUtil.getParentOfType(this, KScrStatementWrapper.class) != null;
	}
	
	public Optional<KScrExpression> initializer(){
		if(getStub() != null)
			return Optional.empty();
		return PsiUtils.childOfType(this, KScrExpression.class);
	}
	
	public boolean hasInferredType(){
		if(getStub() != null)
			return false;
		return PsiUtils.childOfType(this, KScrTypeRef.class)
				.map(x -> x.getText().equals("var") || x.getText().equals("val"))
				.orElse(false);
	}
	
	public @NotNull SearchScope getUseScope(){
		return (isLocal() || hasModifier("private")) ? new LocalSearchScope(getContainingFile()) : super.getUseScope();
	}
	
	public @Nullable Icon getIcon(int flags){
		if(isLocal())
			return PlatformIcons.VARIABLE_ICON;
		else
			return PlatformIcons.FIELD_ICON;
	}
	
	public String getName(){
		var stub = getStub();
		if(stub != null)
			return stub.name();
		return super.getName();
	}
}