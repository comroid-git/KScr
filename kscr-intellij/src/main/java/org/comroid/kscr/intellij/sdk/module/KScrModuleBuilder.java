package org.comroid.kscr.intellij.sdk.module;

import com.intellij.ide.NewProjectWizardLegacy;
import com.intellij.ide.util.projectWizard.*;
import com.intellij.openapi.module.ModifiableModuleModel;
import com.intellij.openapi.module.Module;
import com.intellij.openapi.module.ModuleType;
import com.intellij.openapi.options.ConfigurationException;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.project.ProjectManager;
import com.intellij.openapi.projectRoots.Sdk;
import com.intellij.openapi.projectRoots.SdkTypeId;
import com.intellij.openapi.roots.*;
import com.intellij.openapi.roots.libraries.Library;
import com.intellij.openapi.roots.libraries.LibraryTable;
import com.intellij.openapi.roots.ui.configuration.ModulesProvider;
import com.intellij.openapi.util.Pair;
import com.intellij.openapi.vfs.LocalFileSystem;
import com.intellij.openapi.vfs.VirtualFile;
import org.comroid.kscr.intellij.sdk.KScrSdkType;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

public class KScrModuleBuilder extends ModuleBuilder implements SourcePathsBuilder, ModuleBuilderListener {
    private final KScrModuleType moduleType;
    // Pair<Source Path, Package Prefix>
    private List<Pair<String,String>> srcPaths;
    // Pair<Library path, Source path>
    private List<Pair<String,String>> libraries;

    @Override
    public List<Pair<String, String>> getSourcePaths() throws ConfigurationException {
        if (srcPaths == null){
            final List<Pair<String, String>> paths = new ArrayList<>();
            final String path = getContentEntryPath() + File.separator + "src" + File.separator + "main";
            new File(path).mkdirs();
            paths.add(Pair.create(path, ""));
            return paths;
        }
        return srcPaths;
    }

    @Override
    public void setSourcePaths(List<Pair<String, String>> sourcePaths) {
        srcPaths = sourcePaths == null ? null : new ArrayList<>(sourcePaths);
    }

    @Override
    public void addSourcePath(Pair<String, String> sourcePathInfo) {
        if (srcPaths == null)
            srcPaths = new ArrayList<>();
        srcPaths.add(sourcePathInfo);
    }

    @Override
    public boolean isAvailable() {
        return NewProjectWizardLegacy.isAvailable();
    }

    @Override
    public boolean isSuitableSdkType(SdkTypeId sdkType) {
        return sdkType instanceof KScrSdkType;
    }

    public KScrModuleBuilder(KScrModuleType moduleType) {
        this.moduleType = moduleType;
    }

    @Override
    public ModuleType<?> getModuleType() {
        return moduleType;
    }

    @Override
    public ModuleWizardStep[] createWizardSteps(@NotNull WizardContext wizardContext, @NotNull ModulesProvider modulesProvider) {

    }

    @Override
    public void setupRootModel(@NotNull ModifiableRootModel rootModel) throws ConfigurationException {
        final CompilerModuleExtension compilerModuleExtension = rootModel.getModuleExtension(CompilerModuleExtension.class);
        compilerModuleExtension.setExcludeOutput(true);
        if (myJdk != null){
            rootModel.setSdk(myJdk);
        } else {
            rootModel.inheritSdk();
        }

        ContentEntry contentEntry = doAddContentEntry(rootModel);
        if (contentEntry != null) {
            final List<Pair<String,String>> sourcePaths = getSourcePaths();

            if (sourcePaths != null) {
                for (final Pair<String, String> sourcePath : sourcePaths) {
                    String first = sourcePath.first;
                    new File(first).mkdirs();
                    final VirtualFile sourceRoot = LocalFileSystem.getInstance()
                            .refreshAndFindFileByPath(FileUtil.toSystemIndependentName(first));
                    if (sourceRoot != null) {
                        contentEntry.addSourceFolder(sourceRoot, false, sourcePath.second);
                    }
                }
            }
        }

        if (myCompilerOutputPath != null) {
            // should set only absolute paths
            String canonicalPath;
            try {
                canonicalPath = FileUtil.resolveShortWindowsName(myCompilerOutputPath);
            }
            catch (IOException e) {
                canonicalPath = myCompilerOutputPath;
            }
            compilerModuleExtension
                    .setCompilerOutputPath(VfsUtilCore.pathToUrl(canonicalPath));
        }
        else {
            compilerModuleExtension.inheritCompilerOutputPath(true);
        }

        LibraryTable libraryTable = rootModel.getModuleLibraryTable();
        for (Pair<String, String> libInfo : myModuleLibraries) {
            final String moduleLibraryPath = libInfo.first;
            final String sourceLibraryPath = libInfo.second;
            Library library = libraryTable.createLibrary();
            Library.ModifiableModel modifiableModel = library.getModifiableModel();
            modifiableModel.addRoot(getUrlByPath(moduleLibraryPath), OrderRootType.CLASSES);
            if (sourceLibraryPath != null) {
                modifiableModel.addRoot(getUrlByPath(sourceLibraryPath), OrderRootType.SOURCES);
            }
            modifiableModel.commit();
        }
    }

    @Nullable
    @Override
    public List<Module> commit(@NotNull Project project, ModifiableModuleModel model, ModulesProvider modulesProvider) {
        LanguageLevelProjectExtension extension = LanguageLevelProjectExtension.getInstance(ProjectManager.getInstance().getDefaultProject());
        Boolean aDefault = extension.getDefault();
        LOG.debug("commit: aDefault=" + aDefault);
        LanguageLevelProjectExtension instance = LanguageLevelProjectExtension.getInstance(project);
        if (aDefault != null && !aDefault) {
            instance.setLanguageLevel(extension.getLanguageLevel());
        }
        else {
            //setup language level according to jdk, then setup default flag
            Sdk sdk = ProjectRootManager.getInstance(project).getProjectSdk();
            LOG.debug("commit: projectSdk=" + sdk);
            if (sdk != null) {
                JavaSdkVersion version = JavaSdk.getInstance().getVersion(sdk);
                LOG.debug("commit: sdk.version=" + version);
                if (version != null) {
                    instance.setLanguageLevel(version.getMaxLanguageLevel());
                    instance.setDefault(true);
                }
            }
        }
        return super.commit(project, model, modulesProvider);
    }

    @Override
    public void moduleCreated(@NotNull Module module) {
    }
}
