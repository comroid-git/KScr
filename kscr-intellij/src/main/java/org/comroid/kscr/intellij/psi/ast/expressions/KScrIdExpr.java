package org.comroid.kscr.intellij.psi.ast.expressions;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.lang.jvm.JvmField;
import com.intellij.lang.jvm.types.JvmReferenceType;
import com.intellij.lang.jvm.types.JvmType;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.util.NlsSafe;
import com.intellij.openapi.util.TextRange;
import com.intellij.psi.*;
import com.intellij.psi.util.PsiTreeUtil;
import com.intellij.util.IncorrectOperationException;
import org.comroid.kscr.intellij.antlr_generated.KScrlicLangLexer;
import org.comroid.kscr.intellij.psi.*;
import org.comroid.kscr.intellij.psi.ast.KScrIdPart;
import org.comroid.kscr.intellij.psi.ast.types.KScrType;
import org.comroid.kscr.intellij.psi.types.ClassTypeImpl;
import org.comroid.kscr.intellij.psi.types.JvmKScrlicField;
import org.comroid.kscr.intellij.psi.utils.ProjectTypeFinder;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Arrays;

@SuppressWarnings("UnstableApiUsage")
public class KScrIdExpr extends KScrExpression implements PsiReference, KScrClassReference{
	
	public KScrIdExpr(@NotNull ASTNode node){
		super(node);
	}
	
	/*
	 Can resolve to:
	 - a local variable (int x = 0; int y = x;)
	 - a type (KScrExpression)
	 - a field (KScrExpression.COMPOSITE_ELEMENT_WRAPPER)
	 - a package (org.comroid.kscr.intellij.psi.elements.KScrExpression)
	*/
	
	public String id(){
		return PsiUtils.childOfType(this, KScrIdPart.class).map(PsiElement::getText).orElse("");
	}
	
	@Nullable
	public KScrExpression on(){
		return PsiUtils.childOfType(this, KScrExpression.class).orElse(null);
	}
	
	@Nullable
	public PsiElement dot(){
		return Arrays.stream(getChildren())
				.filter(x -> x.getNode().getElementType() == Tokens.getFor(KScrlicLangLexer.DOT))
				.findFirst()
				.orElse(null);
	}
	
	// [JvmClass | PsiPackage | KScrVariable | JvmField | String | null]
	public Object resolveTarget(){
		var on = on();
		String id = id();
		if(on == null){
			var scope = KScrVarScope.scopeOf(this);
			if(scope.isPresent()){
				var available = scope.get().available();
				var byName = available.stream().filter(x -> x.varName().equals(id)).findFirst();
				if(byName.isPresent())
					return byName.get();
			}
			KScrType inside = PsiTreeUtil.getParentOfType(this, KScrType.class);
			var inMethod = PsiTreeUtil.getParentOfType(this, KScrCodeHolder.class);
			if(inside != null){
				var field = inside.fields().stream()
						.filter(x -> x.varName().equals(id))
						.filter(x -> inMethod == null || !inMethod.isStatic() || x.hasModifier("static"))
						.findFirst()
						.orElse(null);
				if(field != null)
					return field;
			}
			return resolveById(id, getContainingFile(), getProject());
		}else{
			Object ret = null;
			if(on instanceof KScrIdExpr){
				var res = ((KScrIdExpr)on).resolveTarget();
				if(res == null)
					return null;
				String narrowId = id;
				if(res instanceof String)
					narrowId = res + "." + narrowId;
				if(res instanceof PsiPackage)
					narrowId = ((PsiPackage)res).getQualifiedName() + "." + narrowId;
				ret = resolveById(narrowId, getContainingFile(), getProject());
			}
			JvmType type = on.type();
			if(!(type instanceof JvmReferenceType))
				return ret;
			var res = ((JvmReferenceType)type).resolve();
			if(!(res instanceof JvmClass))
				return ret;
			return Arrays.stream(((JvmClass)res).getFields())
					.filter(x -> x.getName().equals(id))
					.findFirst()
					.map(Object.class::cast)
					.orElse(ret);
		}
	}
	
	@NotNull
	private static Object resolveById(String id, PsiFile file, Project project){
		if(file instanceof KScrFile){
			var type = ProjectTypeFinder.firstType(project, ProjectTypeFinder.getCandidates((KScrFile)file, id));
			if(type != null)
				return type;
		}
		var psiPackage = JavaPsiFacade.getInstance(project).findPackage(id);
		if(psiPackage != null)
			return psiPackage;
		return id;
	}
	
	public @Nullable JvmType type(){
		var res = resolveTarget();
		if(res instanceof JvmClass)
			return ClassTypeImpl.of((JvmClass)res);
		if(res instanceof KScrVariable)
			return ((KScrVariable)res).varType();
		if(res instanceof JvmField)
			return ((JvmField)res).getType();
		return null;
	}
	
	public PsiReference getReference(){
		return this;
	}
	
	public @NotNull PsiElement getElement(){
		return this;
	}
	
	public @NotNull TextRange getRangeInElement(){
		return PsiUtils.childOfType(this, KScrIdPart.class).map(PsiElement::getTextRangeInParent).orElse(getTextRangeInParent());
	}
	
	public @Nullable PsiElement resolve(){
		var res = resolveTarget();
		if(res instanceof JvmClass)
			return ((JvmClass)res).getSourceElement();
		if(res instanceof PsiElement)
			return (PsiElement)res;
		if(res instanceof JvmKScrlicField)
			return ((JvmKScrlicField)res).getSourceElement();
		return null;
	}
	
	public @NotNull @NlsSafe String getCanonicalText(){
		var res = resolveTarget();
		if(res instanceof JvmClass){
			var qName = ((JvmClass)res).getQualifiedName();
			return qName != null ? qName : "<anonymous>";
		}
		if(res instanceof PsiPackage)
			return ((PsiPackage)res).getQualifiedName();
		if(res instanceof KScrVariable)
			return ((KScrVariable)res).varName();
		return "<invalid reference>";
	}
	
	public PsiElement handleElementRename(@NotNull String name) throws IncorrectOperationException{
		PsiUtils.childOfType(this, KScrIdPart.class).ifPresent(id -> id.replace(PsiUtils.createIdPartFromText(this, name)));
		return this;
	}
	
	public PsiElement bindToElement(@NotNull PsiElement element) throws IncorrectOperationException{
		if(element instanceof PsiNamedElement){
			PsiUtils.childOfType(this, KScrIdPart.class).ifPresent(id -> id.replace(PsiUtils.createIdPartFromText(this, ((PsiNamedElement)element).getName())));
			return this;
		}
		throw new IncorrectOperationException("Can't bind an ID expression to something that has no name!");
	}
	
	public boolean isReferenceTo(@NotNull PsiElement element){
		var res = resolveTarget();
		if(res instanceof JvmClass)
			return ((JvmClass)res).getSourceElement() == element;
		if(res instanceof PsiElement)
			return res == element;
		if(res instanceof JvmField)
			return ((JvmField)res).getSourceElement() == element;
		return false;
	}
	
	public boolean isSoft(){
		return false;
	}
	
	public @Nullable JvmClass resolveClass(){
		var ref = resolveTarget();
		if(ref instanceof JvmClass)
			return (JvmClass)ref;
		return null;
	}
	
	public boolean isQualified(){
		return on() != null;
	}
	
	public @Nullable KScrFile containingKScrlicFile(){
		var file = getContainingFile();
		return file instanceof KScrFile ? (KScrFile)file : null;
	}
	
	public void shortenReference(){
		var on = on();
		if(on != null)
			on.delete();
		var dot = dot();
		if(dot != null)
			dot.delete();
	}
	
	public @Nullable TextRange getQualifierRange(){
		var on = on();
		if(on == null)
			return null;
		var dot = dot();
		if(dot == null)
			return on.getTextRangeInParent();
		return on.getTextRangeInParent().union(dot.getTextRangeInParent());
	}
}