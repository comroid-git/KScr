package org.comroid.kscr.intellij.psi.types;

import com.intellij.lang.jvm.JvmAnnotation;
import com.intellij.lang.jvm.types.JvmArrayType;
import com.intellij.lang.jvm.types.JvmType;
import org.jetbrains.annotations.NotNull;

import java.util.Map;
import java.util.WeakHashMap;

public class ArrayTypeImpl implements JvmArrayType{
	
	private static final Map<JvmType, ArrayTypeImpl> CACHE = new WeakHashMap<>();
	
	private final JvmType component;
	
	private ArrayTypeImpl(JvmType component){
		this.component = component;
	}
	
	public static ArrayTypeImpl of(JvmType type){
		if(type == null)
			return null;
		return CACHE.computeIfAbsent(type, ArrayTypeImpl::new);
	}
	
	public @NotNull JvmType getComponentType(){
		return component;
	}
	
	public JvmAnnotation @NotNull [] getAnnotations(){
		return new JvmAnnotation[0];
	}
}