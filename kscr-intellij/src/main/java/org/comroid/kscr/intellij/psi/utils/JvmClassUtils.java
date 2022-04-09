package org.comroid.kscr.intellij.psi.utils;

import com.intellij.lang.jvm.JvmClass;
import com.intellij.lang.jvm.JvmMethod;
import com.intellij.lang.jvm.types.*;
import com.intellij.openapi.project.Project;
import com.intellij.psi.JavaPsiFacade;
import com.intellij.psi.search.GlobalSearchScope;
import org.comroid.kscr.intellij.psi.types.ClassTypeImpl;
import org.comroid.kscr.intellij.psi.types.JvmKScrClass;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.List;

import static com.intellij.lang.jvm.types.JvmPrimitiveTypeKind.*;

public class JvmClassUtils{
	
	@NotNull
	public static String getPackageName(JvmClass jClass){
		String name = jClass.getName(), qName = jClass.getQualifiedName();
		if(name == null || qName == null)
			return "";
		return qName.substring(0, qName.length() - 1 - name.length());
	}
	
	@Nullable
	public static JvmType getByName(String name, Project in){
		return ClassTypeImpl.of(JavaPsiFacade.getInstance(in).findClass(name, GlobalSearchScope.everythingScope(in)));
	}
	
	@Nullable
	public static JvmClass asClass(KScrType type){
		return JvmKScrClass.of(type);
	}
	
	@Nullable
	public static JvmType asType(KScrType type){
		return ClassTypeImpl.of(JvmKScrClass.of(type));
	}
	
	@NotNull
	public static String name(JvmType type){
		if(type instanceof JvmArrayType)
			return name(((JvmArrayType)type).getComponentType()) + "[]";
		if(type instanceof JvmPrimitiveType)
			return ((JvmPrimitiveType)type).getKind().getName();
		if(type instanceof JvmReferenceType)
			return ((JvmReferenceType)type).getName();
		return "";
	}
	
	@Nullable
	public static JvmClass asClass(JvmType type){
		if(type instanceof JvmReferenceType){
			var res = ((JvmReferenceType)type).resolve();
			return res instanceof JvmClass ? (JvmClass)res : null;
		}
		return null;
	}
	
	@NotNull
	public static List<JvmMethod> getMethods(@Nullable JvmType type){
		var clss = asClass(type);
		if(clss == null)
			return List.of();
		// TODO: array #clone
		return List.of(clss.getMethods());
	}
	
	public static boolean isAssignableTo(@Nullable JvmType value, @Nullable JvmType to){
		if(value == null || to == null)
			return to == value;
		if(to instanceof JvmPrimitiveType)
			return value instanceof JvmPrimitiveType && (((JvmPrimitiveType)value).getKind() == ((JvmPrimitiveType)to).getKind());
		if(to instanceof JvmArrayType)
			return value instanceof JvmArrayType && (isAssignableTo(((JvmArrayType)value).getComponentType(), ((JvmArrayType)to).getComponentType()));
		if(to instanceof JvmReferenceType){
			var toClass = asClass(to);
			if(toClass != null){
				if(toClass.getQualifiedName() != null && toClass.getQualifiedName().equals("java.lang.Object"))
					return !(value instanceof JvmPrimitiveType);
				return isClassAssignableTo(asClass(value), toClass);
			}
		}
		return false;
	}
	
	public static boolean isConvertibleTo(@Nullable JvmType value, @Nullable JvmType to){
		if(value == null || to == null)
			return to == value;
		if(to instanceof JvmPrimitiveType){
			if(value instanceof JvmPrimitiveType){
				var k = ((JvmPrimitiveType)value).getKind();
				var tk = ((JvmPrimitiveType)to).getKind();
				// why isn't this an enum :p
				if(tk == SHORT || tk == CHAR)
					return k == BYTE || k == SHORT || k == CHAR;
				if(tk == INT)
					return k == BYTE || k == SHORT || k == CHAR || k == INT;
				if(tk == LONG)
					return k == BYTE || k == SHORT || k == CHAR || k == INT || k == LONG;
				if(tk == FLOAT)
					return k == BYTE || k == SHORT || k == CHAR || k == INT || k == LONG || k == FLOAT;
				if(tk == DOUBLE)
					return k == BYTE || k == SHORT || k == CHAR || k == INT || k == LONG || k == FLOAT || k == DOUBLE;
				return false;
			}
			var c = asClass(value);
			if(c != null){
				var name = c.getQualifiedName();
				return name != null && name.equals(((JvmPrimitiveType)to).getKind().getBoxedFqn());
			}
		}
		if(to instanceof JvmArrayType) // Integer[] != int[]
			return value instanceof JvmArrayType && (isAssignableTo(((JvmArrayType)value).getComponentType(), ((JvmArrayType)to).getComponentType()));
		if(to instanceof JvmReferenceType){
			var toClass = asClass(to);
			if(toClass != null){
				if(toClass.getQualifiedName() != null && toClass.getQualifiedName().equals("java.lang.Object"))
					return !(value instanceof JvmPrimitiveType);
				return isClassAssignableTo(asClass(value), toClass);
			}
		}
		return false;
	}
	
	public static boolean isClassAssignableTo(JvmClass value, JvmClass to){
		if(value != null){
			var qualName = value.getQualifiedName();
			if(qualName != null && qualName.equals(to.getQualifiedName()))
				return true;
			if(isClassAssignableTo(asClass(value.getSuperClassType()), to))
				return true;
			for(JvmReferenceType type : value.getInterfaceTypes())
				if(isClassAssignableTo(asClass(type), to))
					return true;
		}
		return false;
	}
}