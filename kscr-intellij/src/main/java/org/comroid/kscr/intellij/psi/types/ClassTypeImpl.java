package org.comroid.kscr.intellij.psi.types;

import com.intellij.lang.jvm.JvmAnnotation;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.lang.jvm.JvmTypeDeclaration;
import com.intellij.lang.jvm.JvmTypeParameter;
import com.intellij.lang.jvm.types.JvmReferenceType;
import com.intellij.lang.jvm.types.JvmSubstitutor;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.lang.jvm.types.JvmTypeResolveResult;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.WeakHashMap;

public class ClassTypeImpl implements JvmReferenceType{
	
	private static final Map<JvmClass, ClassTypeImpl> CACHE = new WeakHashMap<>();
	
	private final JvmClass underlying;
	
	private ClassTypeImpl(JvmClass underlying){
		this.underlying = underlying;
	}
	
	@Nullable
	public static ClassTypeImpl of(JvmClass jClass){
		if(jClass == null)
			return null;
		return CACHE.computeIfAbsent(jClass, ClassTypeImpl::new);
	}
	
	public @NotNull String getName(){
		var name = underlying.getName();
		return name != null ? name : "<anonymous>";
	}
	
	public @Nullable JvmTypeResolveResult resolveType(){
		return new JvmTypeResolveResult(){
			public @NotNull JvmTypeDeclaration getDeclaration(){
				return underlying;
			}
			
			public @NotNull JvmSubstitutor getSubstitutor(){
				return new JvmSubstitutor(){
					public @NotNull Collection<JvmTypeParameter> getTypeParameters(){
						return List.of();
					}
					
					public @Nullable JvmType substitute(@NotNull JvmTypeParameter typeParameter){
						return null;
					}
				};
			}
		};
	}
	
	public @NotNull Iterable<JvmType> typeArguments(){
		return List.of();
	}
	
	public JvmAnnotation @NotNull [] getAnnotations(){
		return new JvmAnnotation[0];
	}
}