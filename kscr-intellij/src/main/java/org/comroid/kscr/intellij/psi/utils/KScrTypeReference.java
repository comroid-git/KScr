package org.comroid.kscr.intellij.psi.utils;

import com.intellij.codeInsight.lookup.LookupElementBuilder;
import com.intellij.codeInspection.LocalQuickFix;
import com.intellij.codeInspection.LocalQuickFixProvider;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.lang.jvm.JvmClassKind;
import com.intellij.lang.jvm.JvmModifier;
import com.intellij.openapi.util.TextRange;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiNamedElement;
import com.intellij.psi.PsiReference;
import com.intellij.psi.search.GlobalSearchScope;
import com.intellij.psi.search.PsiShortNamesCache;
import com.intellij.psi.util.PsiTreeUtil;
import com.intellij.ui.JBColor;
import com.intellij.util.IncorrectOperationException;
import org.comroid.kscr.intellij.inspections.fixes.AddImportFix;
import org.comroid.kscr.intellij.psi.*;
import org.comroid.kscr.intellij.psi.types.KScrKind;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.*;
import java.util.function.Predicate;
import java.util.stream.Collectors;

public class KScrTypeReference implements PsiReference, LocalQuickFixProvider{
	
	KScrId id;
	KScrIdHolder from;
	
	public KScrTypeReference(KScrId id, KScrIdHolder from){
		this.id = id;
		this.from = from;
	}
	
	public @NotNull PsiElement getElement(){
		return from;
	}
	
	public @NotNull TextRange getRangeInElement(){
		return id.getTextRangeInParent();
	}
	
	public @Nullable JvmClass resolveClass(){
		if(id == null)
			return null;
		var p = id.getProject();
		// possible names come from imports
		var file = from.getContainingFile();
		if(file instanceof KScrFile)
			return ProjectTypeFinder.firstType(p, getCandidates((KScrFile)file, id.getText()));
		return ProjectTypeFinder.findByName(p, id.getText()).orElse(null);
	}
	
	@NotNull
	public static List<String> getCandidates(KScrFile file, String id){
		Optional<String> pkg = file.getPackage().map(KScrPackageStatement::getPackageName);
		List<String> candidates = file.getImports().stream()
				.filter(x -> !x.isStatic())
				.map(x -> x.isWildcard() ? (x.getImportName() + "." + id) : (x.getImportName().endsWith(id) ? x.getImportName() : null))
				.filter(Objects::nonNull)
				.collect(Collectors.toList());
		candidates.add(0, id);
		// TODO: all implicit imports
		candidates.add(1, "java.lang." + id);
		pkg.ifPresent(s -> candidates.add(2, s + "." + id));
		return candidates;
	}
	
	public @Nullable PsiElement resolve(){
		var cClass = resolveClass();
		return cClass != null ? cClass.getSourceElement() : null;
	}
	
	public @NotNull String getCanonicalText(){
		var cClass = resolveClass();
		return cClass != null ? cClass.getQualifiedName() : id.getText();
	}
	
	public PsiElement handleElementRename(@NotNull String name) throws IncorrectOperationException{
		from.setName(name);
		return from;
	}
	
	public PsiElement bindToElement(@NotNull PsiElement element) throws IncorrectOperationException{
		if(element instanceof KScrType){
			from.setName(((KScrType)element).fullyQualifiedName());
			return from;
		}
		throw new IncorrectOperationException("Can't bind a KScrBaseTypeRef to an element that is not a KScrTypeDef");
	}
	
	public boolean isReferenceTo(@NotNull PsiElement element){
		if(element instanceof KScrType)
			return matchesType((KScrType)element);
		return resolve() == element;
	}
	
	public boolean isSoft(){
		return false;
	}
	
	public boolean matchesType(KScrType typeDef){
		String ourId = id.getText();
		if(ourId == null || ourId.isBlank())
			return false;
		if(from.getContainingFile() instanceof KScrFile){
			KScrFile file = (KScrFile)from.getContainingFile();
			Optional<String> pkg = file.getPackage().map(KScrPackageStatement::getPackageName);
			if(!pkg.map(String::isBlank).orElse(true) && typeDef.getPackageName().isBlank()){
				// we're not in the default package -> we can't reference it
				return false;
			}
			String fqName = typeDef.fullyQualifiedName();
			// check if
			// its FQ-name == our text (plus package name)
			if(fqName.equals(ourId) || (pkg.isPresent() && (pkg.get() + "." + ourId).equals(fqName)))
				return true;
			// it's FQ-name == some import that ends in our text,
			// it's FQ-name == a wildcard import + our text
			//     TODO: static imports for inner types
			//     TODO: also consider target's visibility
			for(KScrImportStatement i : file.getImports())
				if(!i.isStatic()){
					String name = i.getImportName();
					if(i.isWildcard() && fqName.equals(name + "." + ourId))
						return true;
					else if(name.endsWith(ourId) && fqName.equals(name))
						return true;
				}
			return false;
		}else
			return typeDef.fullyQualifiedName().equals(ourId);
	}
	
	public LocalQuickFix @Nullable [] getQuickFixes(){
		// we resolve to nothing, see if there exists a type with the right short name
		var shortName = id.getText();
		if(shortName.contains("."))
			return new LocalQuickFix[0];
		
		var project = id.getProject();
		List<JvmClass> candidates = ProjectTypeFinder.findAll(project, x -> x.getName().equals(shortName));
		candidates.addAll(Arrays.asList(PsiShortNamesCache.getInstance(project).getClassesByName(shortName, GlobalSearchScope.everythingScope(project))));
		
		return candidates.stream()
				.map(x -> new AddImportFix(x.getQualifiedName(), id))
				.toArray(LocalQuickFix[]::new);
	}
	
	public Object @NotNull [] getVariants(){
		Predicate<JvmClass> isWrongClause = (aClass) -> {
			KScrType in;
			if(PsiTreeUtil.getParentOfType(id, KScrImplementsClause.class) != null)
				if(aClass.getClassKind() != JvmClassKind.INTERFACE)
					return true;
			if(PsiTreeUtil.getParentOfType(id, KScrExtendsClause.class) != null && (in = PsiTreeUtil.getParentOfType(id, KScrType.class)) != null)
				return aClass.hasModifier(JvmModifier.FINAL)
						|| (in.kind() != KScrKind.INTERFACE && aClass.getClassKind() == JvmClassKind.INTERFACE)
						|| (in.kind() == KScrKind.INTERFACE && aClass.getClassKind() != JvmClassKind.INTERFACE)
						|| aClass.getClassKind() == JvmClassKind.ANNOTATION;
			return false;
		};
		
		return fillCompletion(id, isWrongClause);
	}
	
	public static Object @NotNull [] fillCompletion(KScrElement at, Predicate<JvmClass> isWrongClause){
		List<LookupElementBuilder> list = new ArrayList<>();
		for(JvmClass aClass : ProjectTypeFinder.allVisibleAt(at.getProject(), at)){
			boolean wrongClause = isWrongClause.test(aClass);
			PsiElement decl = aClass.getSourceElement();
			if(decl != null){
				var fqName = aClass.getQualifiedName();
				LookupElementBuilder builder = LookupElementBuilder
						.createWithIcon((PsiNamedElement)decl)
						.withTailText(" " + JvmClassUtils.getPackageName(aClass))
						.withPsiElement(aClass.getSourceElement())
						.withInsertHandler((ctx, elem) -> {
							if(ctx.getFile() instanceof KScrFile){
								KScrFile cFile = (KScrFile)ctx.getFile();
								if(cFile.getImports().stream().noneMatch(imp -> imp.importsType(aClass))
										&& !KScrImportStatement.importsType("java.lang.*", fqName)
										&& !KScrImportStatement.importsType(cFile.getPackageName() + ".*", fqName))
									AddImportFix.addImport(cFile, fqName);
							}
						});
				if(wrongClause)
					builder = builder.withItemTextForeground(JBColor.RED);
				list.add(builder);
			}
		}
		return list.toArray();
	}
}