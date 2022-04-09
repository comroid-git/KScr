package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiReference;
import com.intellij.psi.PsiType;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.KScrIdHolder;
import org.comroid.kscr.intellij.psi.types.ClassTypeImpl;
import org.comroid.kscr.intellij.psi.utils.KScrTypeReference;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class KScrRawTypeRef extends KScrAstElement implements KScrIdHolder{
	
	public KScrRawTypeRef(@NotNull ASTNode node){
		super(node);
	}
	
	public PsiReference getReference(){
		return getIdElement().map(id -> new KScrTypeReference(id, this)).orElse(null);
	}
	
	@Nullable
	public JvmType type(){
		switch(getText()){
			case "boolean":
				return PsiType.BOOLEAN;
			case "byte":
				return PsiType.BYTE;
			case "short":
				return PsiType.SHORT;
			case "char":
				return PsiType.CHAR;
			case "int":
				return PsiType.INT;
			case "long":
				return PsiType.LONG;
			case "float":
				return PsiType.FLOAT;
			case "double":
				return PsiType.DOUBLE;
			case "void":
				return PsiType.VOID;
		}
		var ref = getReference();
		if(ref instanceof KScrTypeReference)
			return ClassTypeImpl.of(((KScrTypeReference)ref).resolveClass());
		return null;
	}
}