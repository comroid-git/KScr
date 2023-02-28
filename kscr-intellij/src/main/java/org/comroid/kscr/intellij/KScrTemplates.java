package org.comroid.kscr.intellij;

import com.intellij.ide.fileTemplates.DefaultCreateFromTemplateHandler;
import com.intellij.ide.fileTemplates.FileTemplate;
import com.intellij.openapi.project.Project;
import com.intellij.psi.PsiDirectory;
import com.intellij.psi.PsiElement;
import com.intellij.util.IncorrectOperationException;
import org.jetbrains.annotations.NotNull;

import java.util.Map;

public class KScrTemplates extends DefaultCreateFromTemplateHandler{
	
	@Override
	public boolean handlesTemplate(@NotNull FileTemplate template){
		return template.isTemplateOfType(KScrFileType.SOURCE);
	}
	
	public @NotNull PsiElement createFromTemplate(@NotNull Project project, @NotNull PsiDirectory directory, String fileName, @NotNull FileTemplate template, @NotNull String templateText, @NotNull Map<String, Object> props) throws IncorrectOperationException{
		fileName += ".kscr";
		return super.createFromTemplate(project, directory, fileName, template, templateText, props);
	}
	
	public boolean canCreate(PsiDirectory @NotNull [] dirs){
		// don't show internal templates in the right click menu outside the KScr Class menu
		return false;
	}
}