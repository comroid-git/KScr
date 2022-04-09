package org.comroid.kscr.intellij.psi.ast.common;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiType;
import com.intellij.psi.search.LocalSearchScope;
import com.intellij.psi.search.SearchScope;
import com.intellij.util.PlatformIcons;
import org.comroid.kscr.intellij.antlr_generated.KScrlicLangLexer;
import org.comroid.kscr.intellij.psi.KScrDefinitionStubElement;
import org.comroid.kscr.intellij.psi.KScrVariable;
import org.comroid.kscr.intellij.psi.Tokens;
import org.comroid.kscr.intellij.psi.ast.KScrTypeRef;
import org.comroid.kscr.intellij.psi.ast.types.KScrRecordComponents;
import org.comroid.kscr.intellij.psi.stubs.StubKScrParameter;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.comroid.kscr.intellij.psi.types.ArrayTypeImpl;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;
import java.util.Optional;

public class KScrParameter extends KScrDefinitionStubElement<KScrParameter, StubKScrParameter> implements KScrVariable{
	
	public KScrParameter(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrParameter(@NotNull StubKScrParameter parameter){
		super(parameter, StubTypes.KSCR_PARAMETER);
	}
	
	public String varName(){
		return getName();
	}
	
	public String getName(){
		var stub = getStub();
		if(stub != null)
			return stub.name();
		return super.getName();
	}
	
	public JvmType varType(){
		var type = getTypeName()
				.map(KScrTypeRef::asType)
				.orElse(PsiType.NULL);
		return isVarargs() ? ArrayTypeImpl.of(type) : type;
	}
	
	public boolean hasModifier(String modifier){
		var stub = getStub();
		if(stub != null)
			return stub.hasModifier(modifier);
		
		if(modifier.equals("final"))
			return getNode().findChildByType(Tokens.getFor(KScrlicLangLexer.FINAL)) != null;
		if(modifier.equals("private"))
			return getParent() instanceof KScrRecordComponents;
		return false;
	}
	
	@NotNull
	public Optional<KScrTypeRef> getTypeName(){
		var stub = getStub();
		if(stub != null){
			var type = PsiUtils.createTypeReferenceFromText(this, stub.typeText());
			return Optional.of((KScrTypeRef)type);
		}
		
		return PsiUtils.childOfType(this, KScrTypeRef.class);
	}
	
	public boolean isMethodParameter(){
		var stub = getStub();
		if(stub != null)
			return !stub.isRecordComponent();
		
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
	
	public boolean isVarargs(){
		var stub = getStub();
		if(stub != null)
			return stub.isVarargs();
		
		return getNode().findChildByType(Tokens.getFor(KScrlicLangLexer.ELIPSES)) != null;
	}
	
	public boolean isLocal(){
		return isMethodParameter();
	}
}