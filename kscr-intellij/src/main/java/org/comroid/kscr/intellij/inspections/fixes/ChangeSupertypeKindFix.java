package org.comroid.kscr.intellij.inspections.fixes;

import com.intellij.codeInspection.LocalQuickFixAndIntentionActionOnPsiElement;
import com.intellij.codeInspection.util.IntentionFamilyName;
import com.intellij.codeInspection.util.IntentionName;
import com.intellij.openapi.editor.Editor;
import com.intellij.openapi.project.Project;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiFile;
import com.intellij.psi.PsiWhiteSpace;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import static org.comroid.kscr.intellij.psi.utils.PsiUtils.*;

public class ChangeSupertypeKindFix extends LocalQuickFixAndIntentionActionOnPsiElement{
	
	private final boolean toExtends;
	
	public ChangeSupertypeKindFix(@Nullable PsiElement element, boolean toExtends){
		super(element);
		this.toExtends = toExtends;
	}
	
	public void invoke(@NotNull Project project, @NotNull PsiFile file, @Nullable Editor editor, @NotNull PsiElement startElement, @NotNull PsiElement endElement){
		PsiElement from = startElement.getParent();
		String type = startElement.getText();
		var targetOpt = toExtends ?
				childOfType(from.getParent(), KScrExtendsClause.class) :
				childOfType(from.getParent(), KScrImplementsClause.class);
		String newClause = targetOpt.map(x -> x.getText() + ", ").orElse(toExtends ? "extends " : "implements ") + type;
		var newElem = toExtends ?
				createExtendsClauseFromText(from.getParent(), newClause) :
				createImplementsClauseFromText(from.getParent(), newClause);
		
		if(targetOpt.isPresent())
			targetOpt.get().replace(newElem);
		else{
			if(toExtends){
				from.getParent().addBefore(newElem, from); from.addAfter(createWhitespace(newElem, " "), null);
			}else{
				from.getParent().addAfter(newElem, from); from.addBefore(createWhitespace(newElem, " "), null);
			}
		}
		
		var others = childrenOfType(from, KScrTypeRef.class);
		if(others.size() == 1)
			from.delete();
		else{
			// clean up commas
			// if we're first, remove first comma, else remove previous one
			if(startElement == others.get(0)){
				while(startElement.getNextSibling() != null && (startElement.getNextSibling() instanceof PsiWhiteSpace || startElement.getNextSibling().getText().equals(",")))
					startElement.getNextSibling().delete();
			}else{
				while(startElement.getPrevSibling() != null && (startElement.getPrevSibling() instanceof PsiWhiteSpace || startElement.getPrevSibling().getText().equals(",")))
					startElement.getPrevSibling().delete();
			}
			startElement.delete();
		}
	}
	
	public @IntentionName @NotNull String getText(){
		return "Change to '" + (toExtends ? "extends" : "implements") + "'";
	}
	
	public @IntentionFamilyName @NotNull String getFamilyName(){
		return "Swap 'extends' and 'implements'";
	}
}