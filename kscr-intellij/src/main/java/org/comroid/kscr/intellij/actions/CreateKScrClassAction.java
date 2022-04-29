package org.comroid.kscr.intellij.actions;

import com.intellij.ide.actions.CreateFileFromTemplateDialog;
import com.intellij.ide.actions.CreateTemplateInPackageAction;
import com.intellij.ide.fileTemplates.FileTemplate;
import com.intellij.ide.fileTemplates.FileTemplateManager;
import com.intellij.ide.fileTemplates.FileTemplateUtil;
import com.intellij.java.JavaBundle;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.util.NlsContexts;
import com.intellij.openapi.util.text.StringUtil;
import com.intellij.psi.*;
import com.intellij.ui.LayeredIcon;
import com.intellij.util.IncorrectOperationException;
import com.intellij.util.PlatformIcons;
import org.comroid.kscr.intellij.KScrIcons;
import org.comroid.kscr.intellij.psi.KScrFile;
import org.comroid.kscr.intellij.psi.ast.types.KScrType;
import org.jetbrains.annotations.NonNls;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;
import org.jetbrains.jps.model.java.JavaModuleSourceRootTypes;

import java.util.Properties;

public class CreateKScrClassAction extends CreateTemplateInPackageAction<KScrType> {

    protected CreateKScrClassAction(){
        super("", "Create new KScr class", new LayeredIcon(PlatformIcons.CLASS_ICON, KScrIcons.KScr_ICON), JavaModuleSourceRootTypes.SOURCES);
    }

    protected @Nullable PsiElement getNavigationElement(@NotNull KScrType createdElement){
        return createdElement;
    }

    protected @Nullable KScrType doCreate(PsiDirectory dir, String className, String templateName) throws IncorrectOperationException{
        Project project = dir.getProject();
        FileTemplate template = FileTemplateManager.getInstance(project).getInternalTemplate(templateName);

        Properties defaultProperties = FileTemplateManager.getInstance(project).getDefaultProperties();
        Properties properties = new Properties(defaultProperties);
        properties.setProperty(FileTemplate.ATTRIBUTE_NAME, className);
		/*for(Map.Entry<String, String> entry : additionalProperties.entrySet())
			properties.setProperty(entry.getKey(), entry.getValue());*/

        String fileName = className + ".kScr";

        PsiElement element;
        try{
            element = FileTemplateUtil.createFromTemplate(template, fileName, properties, dir);
        }catch(Exception e){
            throw new IncorrectOperationException(e);
        }
        KScrFile file = (KScrFile)element.getContainingFile();
        return file.getTypeDef().orElseThrow(IncorrectOperationException::new);
    }

    protected boolean checkPackageExists(PsiDirectory directory){
        PsiPackage pkg = JavaDirectoryService.getInstance().getPackage(directory);
        if(pkg == null)
            return false;

        String name = pkg.getQualifiedName();
        return StringUtil.isEmpty(name) || PsiNameHelper.getInstance(directory.getProject()).isQualifiedName(name);
    }

    protected void buildDialog(@NotNull Project project, @NotNull PsiDirectory directory, CreateFileFromTemplateDialog.@NotNull Builder builder){
        builder
                .setTitle("New KScr Class")
                .addKind("Class", PlatformIcons.CLASS_ICON, "KScr Class")
                .addKind("Interface", PlatformIcons.INTERFACE_ICON, "KScr Interface")
                .addKind("Record", PlatformIcons.RECORD_ICON, "KScr Record")
                .addKind("Enum", PlatformIcons.ENUM_ICON, "KScr Enum")
                .addKind("Annotation", PlatformIcons.ANNOTATION_TYPE_ICON, "KScr Annotation");
    }

    @SuppressWarnings("UnstableApiUsage")
    protected @NlsContexts.Command String getActionName(PsiDirectory directory, @NonNls @NotNull String newName, @NonNls String templateName){
        PsiPackage psiPackage = JavaDirectoryService.getInstance().getPackage(directory);
        return JavaBundle.message("progress.creating.class", StringUtil.getQualifiedName(psiPackage == null ? "" : psiPackage.getQualifiedName(), newName));
    }

    public boolean startInWriteAction(){
        return false;
    }

    protected String removeExtension(String templateName, String className){
        return StringUtil.trimEnd(className, ".kscr");
    }
}
