package org.comroid.kscr.intellij.psi.ast.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiPrimitiveType;
import com.intellij.psi.impl.source.tree.CompositeElement;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.utils.JvmClassUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.lang.reflect.Field;

public class KScrExpression extends KScrAstElement{
	
	public KScrExpression(@NotNull ASTNode node){
		super(node);
	}
	
	public @Nullable JvmType type(){
		return PsiPrimitiveType.NULL;
	}
	
	public boolean isAssignableTo(JvmType type){
		return JvmClassUtils.isAssignableTo(type(), type);
	}
	
	public boolean isConvertibleTo(JvmType type){
		return JvmClassUtils.isConvertibleTo(type(), type);
	}
	
	
	
	private static final Field COMPOSITE_ELEMENT_WRAPPER;
	
	static{
		try{
			COMPOSITE_ELEMENT_WRAPPER = CompositeElement.class.getDeclaredField("myWrapper");
		}catch(NoSuchFieldException e){
			throw new RuntimeException(e);
		}
	}
	
	public void subtreeChanged(){
		super.subtreeChanged();
		// TODO:
		//  Since all expressions share an element type (PSI type is based on sub-trees), the PSI type doesn't get invalidated properly.
		//  The proper fix would be to preserve ANTLR tag information and give each tagged alternative its own element type.
		// setPsi actually checks for a null value, so we set it by reflection
		try{
			COMPOSITE_ELEMENT_WRAPPER.setAccessible(true);
			COMPOSITE_ELEMENT_WRAPPER.set(getNode(), null);
			COMPOSITE_ELEMENT_WRAPPER.setAccessible(false);
		}catch(IllegalAccessException e){
			throw new RuntimeException(e);
		}
	}
}