package org.comroid.kscr.intellij.psi.types;

import com.intellij.lang.jvm.*;
import com.intellij.lang.jvm.types.JvmReferenceType;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiElement;
import org.comroid.kscr.intellij.psi.KScrMethod;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Locale;
import java.util.Map;
import java.util.WeakHashMap;

@SuppressWarnings("UnstableApiUsage")
public class JvmKScrMethod implements JvmMethod{
	
	private static final Map<KScrMethod, JvmKScrMethod> CACHE = new WeakHashMap<>();
	
	private final KScrMethod underlying;
	
	private JvmKScrMethod(KScrMethod underlying){
		this.underlying = underlying;
	}
	
	public static JvmKScrMethod of(KScrMethod method){
		if(method == null)
			return null;
		return CACHE.computeIfAbsent(method, JvmKScrMethod::new);
	}
	
	public boolean isConstructor(){
		return false;
	}
	
	public @Nullable JvmClass getContainingClass(){
		return JvmKScrClass.of(underlying.containingType());
	}
	
	public @NotNull String getName(){
		return underlying.getName();
	}
	
	public @Nullable JvmType getReturnType(){
		return underlying.returnType();
	}
	
	public JvmParameter @NotNull [] getParameters(){
		return underlying.parameters().stream().map(JvmKScrParameter::of).toArray(JvmKScrParameter[]::new);
	}
	
	public boolean isVarArgs(){
		return false;
	}
	
	public JvmReferenceType @NotNull [] getThrowsTypes(){
		return new JvmReferenceType[0];
	}
	
	public JvmTypeParameter @NotNull [] getTypeParameters(){
		return new JvmTypeParameter[0];
	}
	
	public boolean hasModifier(@NotNull JvmModifier modifier){
		if(modifier == JvmModifier.PACKAGE_LOCAL)
			return !(underlying.hasModifier("public") || underlying.hasModifier("protected") || underlying.hasModifier("private"));
		return underlying.hasModifier(modifier.name().toLowerCase(Locale.ROOT));
	}
	
	public JvmAnnotation @NotNull [] getAnnotations(){
		return new JvmAnnotation[0];
	}
	
	public @Nullable PsiElement getSourceElement(){
		return underlying;
	}
}