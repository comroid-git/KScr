package org.comroid.kscr.intellij.psi.ast.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.lang.jvm.types.JvmReferenceType;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiPrimitiveType;
import org.comroid.kscr.intellij.psi.ast.KScrBinaryOp;
import org.comroid.kscr.intellij.psi.utils.JvmClassUtils;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Objects;
import java.util.Optional;

import static org.comroid.kscr.intellij.psi.utils.JvmClassUtils.typeByName;

public class KScrBinaryExpr extends KScrExpression{
	
	public KScrBinaryExpr(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrBinaryOp> op(){
		return PsiUtils.childOfType(this, KScrBinaryOp.class);
	}
	
	public String symbol(){
		return op().map(PsiElement::getText).orElse("");
	}
	
	public Optional<KScrExpression> left(){
		return PsiUtils.childOfType(this, KScrExpression.class, 0);
	}
	
	public Optional<KScrExpression> right(){
		return PsiUtils.childOfType(this, KScrExpression.class, 1);
	}
	
	public @Nullable JvmType type(){
		switch(symbol()){
			case "":
				return null;
			case "+":
				// consider string addition
				var lType = left().map(KScrExpression::type).orElse(null);
				if(lType instanceof JvmReferenceType){
					var resolve = ((JvmReferenceType)lType).resolve();
					if(resolve instanceof JvmClass && Objects.equals(((JvmClass)resolve).getQualifiedName(), "java.lang.String"))
						return typeByName("java.lang.String", getProject());
				}
				var rType = right().map(KScrExpression::type).orElse(null);
				if(rType instanceof JvmReferenceType){
					var resolve = ((JvmReferenceType)rType).resolve();
					if(resolve instanceof JvmClass && Objects.equals(((JvmClass)resolve).getQualifiedName(), "java.lang.String"))
						return typeByName("java.lang.String", getProject());
				}
				// otherwise fall-through
			case "-":
			case "*":
			case "/":
			case "%":
			case "&":
			case "|":
			case "^":
			case "<<":
			case ">>":
			case "<<<":
			case ">>>":
				return JvmClassUtils.highest(
						left().map(KScrExpression::type).orElse(null),
						right().map(KScrExpression::type).orElse(null));
			case "&&":
			case "||":
			case "==":
			case "!=":
			case ">=":
			case "<=":
			case ">":
			case "<":
				return PsiPrimitiveType.BOOLEAN;
			case "|>":
				return right().map(KScrExpression::type).orElse(null);
		}
		return null;
	}
}