package org.comroid.kscr.intellij.psi.ast.types;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.psi.PsiElement;
import com.intellij.psi.stubs.IStubElementType;
import org.comroid.kscr.intellij.psi.KScrStubElement;
import org.comroid.kscr.intellij.psi.ast.KScrTypeRef;
import org.comroid.kscr.intellij.psi.stubs.StubKScrClassList;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.List;
import java.util.Objects;
import java.util.Optional;
import java.util.stream.Collectors;

public abstract class KScrClassList<CL extends KScrClassList<CL>> extends KScrStubElement<CL, StubKScrClassList<CL>>{
	
	public KScrClassList(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrClassList(@NotNull StubKScrClassList<CL> list, @NotNull IStubElementType<?, ?> nodeType){
		super(list, nodeType);
	}
	
	@NotNull
	public List<JvmClass> elements(){
		return PsiUtils
				.streamChildrenOfType(this, KScrTypeRef.class)
				.map(KScrTypeRef::asClass)
				.filter(Objects::nonNull)
				.collect(Collectors.toList());
	}
	
	@NotNull
	public List<String> elementNames(){
		var stub = getStub();
		if(stub != null)
			return stub.elementFqNames();
		
		return PsiUtils
				.streamChildrenOfType(this, KScrTypeRef.class)
				.map(PsiElement::getText)
				.collect(Collectors.toList());
	}
	
	@NotNull
	public Optional<JvmClass> first(){
		return PsiUtils
				.streamChildrenOfType(this, KScrTypeRef.class)
				.map(KScrTypeRef::asClass)
				.filter(Objects::nonNull)
				.findFirst();
	}
}