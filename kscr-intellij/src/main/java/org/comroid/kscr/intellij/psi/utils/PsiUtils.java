package org.comroid.kscr.intellij.psi.utils;

import com.intellij.openapi.project.Project;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiFileFactory;
import com.intellij.psi.impl.PsiFileFactoryImpl;
import com.intellij.psi.tree.IElementType;
import org.comroid.kscr.intellij.KScrLanguage;
import org.comroid.kscr.intellij.antlr_generated.KScrLangLexer;
import org.comroid.kscr.intellij.antlr_generated.KScrLangParser;
import org.comroid.kscr.intellij.psi.Tokens;
import org.jetbrains.annotations.NotNull;

import java.util.Arrays;
import java.util.List;
import java.util.Optional;
import java.util.function.Predicate;
import java.util.stream.Collectors;

public class PsiUtils{
	
	private static PsiElement createFromText(Project project, PsiElement context, String text, IElementType type){
		PsiFileFactoryImpl factory = (PsiFileFactoryImpl)PsiFileFactory.getInstance(project);
		return factory.createElementFromText(text, KScrLanguage.LANGUAGE, type, context);
	}
	
	public static PsiElement createFromText(@NotNull PsiElement context, String text, IElementType elementType){
		return createFromText(context.getProject(), context, text, elementType);
	}
	
	// the ID token is understood in ParserAdaptor to mean a full ID
	public static PsiElement createIdFromText(@NotNull PsiElement context, String text){
		return createFromText(context, text, Tokens.getFor(KScrLangLexer.ID)).getFirstChild();
	}
	
	public static PsiElement createIdPartFromText(@NotNull PsiElement context, String text){
		return createIdFromText(context, text).getFirstChild();
	}
	
	public static PsiElement createExpressionFromText(@NotNull PsiElement context, String text){
		return createFromText(context, text, Tokens.getRuleFor(KScrLangParser.RULE_expr)).getFirstChild();
	}
	
	public static PsiElement createImportFromText(@NotNull PsiElement context, String text){
		return createFromText(context, text, Tokens.getRuleFor(KScrLangParser.RULE_importDecl)).getFirstChild();
	}
	
	public static PsiElement createExtendsClauseFromText(@NotNull PsiElement context, String text){
		return createFromText(context, text, Tokens.getRuleFor(KScrLangParser.RULE_objectExtends)).getFirstChild();
	}
	
	public static PsiElement createImplementsClauseFromText(@NotNull PsiElement context, String text){
		return createFromText(context, text, Tokens.getRuleFor(KScrLangParser.RULE_objectImplements)).getFirstChild();
	}
	
	public static PsiElement createWhitespace(@NotNull PsiElement context, String text){
		assert text.isBlank();
		return createFromText(context, text, Tokens.getFor(KScrLangLexer.ID)).getFirstChild();
	}
	
	@NotNull public static <X> List<X> childrenOfType(@NotNull PsiElement parent, Class<X> filter){
		return Arrays.stream(parent.getChildren())
				.filter(filter::isInstance)
				.map(z -> (X)z)
				.collect(Collectors.toList());
	}
	
	@NotNull public static <X> List<X> wrappedChildrenOfType(@NotNull PsiElement parent, Class<X> filter){
		return Arrays.stream(parent.getChildren())
				.map(PsiElement::getFirstChild)
				.filter(filter::isInstance)
				.map(z -> (X)z)
				.collect(Collectors.toList());
	}
	
	@NotNull public static List<PsiElement> matchingChildren(@NotNull PsiElement parent, Predicate<PsiElement> filter){
		return Arrays.stream(parent.getChildren())
				.filter(filter)
				.collect(Collectors.toList());
	}
	
	@NotNull public static <X extends PsiElement> Optional<X> childOfType(@NotNull PsiElement parent, Class<X> filter){
		return childOfType(parent, filter, 0);
	}
	
	@NotNull public static <X extends PsiElement> Optional<X> childOfType(@NotNull PsiElement parent, Class<X> filter, int index){
		var c = childrenOfType(parent, filter);
		if(c.size() > index)
			return Optional.of(c.get(index));
		else
			return Optional.empty();
	}
	
	@NotNull public static <X extends PsiElement> Optional<X> wrappedChildOfType(@NotNull PsiElement parent, Class<X> filter){
		return wrappedChildOfType(parent, filter, 0);
	}
	
	@NotNull public static <X extends PsiElement> Optional<X> wrappedChildOfType(@NotNull PsiElement parent, Class<X> filter, int index){
		var c = wrappedChildrenOfType(parent, filter);
		if(c.size() > index)
			return Optional.of(c.get(index));
		else
			return Optional.empty();
	}
}