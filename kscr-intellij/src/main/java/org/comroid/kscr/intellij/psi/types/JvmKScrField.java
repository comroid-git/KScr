package org.comroid.kscr.intellij.psi.types;

import com.intellij.lang.jvm.JvmAnnotation;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.lang.jvm.JvmField;
import com.intellij.lang.jvm.JvmModifier;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiElement;
import org.comroid.kscr.intellij.psi.utils.KScrVariable;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Map;
import java.util.WeakHashMap;

@SuppressWarnings("UnstableApiUsage")
public class JvmKScrField implements JvmField{
	
	private static final Map<KScrVariable, JvmKScrField> CACHE = new WeakHashMap<>();
	
	private final KScrVariable underlying;
	
	private JvmKScrField(KScrVariable underlying){
		this.underlying = underlying;
	}
	
	public static JvmKScrField of(KScrVariable var){
		if(var == null)
			return null;
		return CACHE.computeIfAbsent(var, JvmKScrField::new);
	}
	
	public @Nullable JvmClass getContainingClass(){
		return null;
	}
	
	public @NotNull String getName(){
		return underlying.varName();
	}
	
	public @NotNull JvmType getType(){
		return underlying.varType();
	}
	
	public boolean hasModifier(@NotNull JvmModifier modifier){
		return false;
	}
	
	public JvmAnnotation @NotNull [] getAnnotations(){
		return new JvmAnnotation[0];
	}
	
	public @Nullable PsiElement getSourceElement(){
		return underlying.declaration();
	}
}
