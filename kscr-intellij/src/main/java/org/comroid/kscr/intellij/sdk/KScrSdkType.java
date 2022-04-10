package org.comroid.kscr.intellij.sdk;

import com.intellij.openapi.projectRoots.*;
import org.jdom.Element;
import org.jetbrains.annotations.Nls;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.io.File;
import java.util.Arrays;

public class KScrSdkType extends SdkType {
    public static final KScrSdkType INSTANCE = new KScrSdkType();

    public KScrSdkType() {
        super("KScrSDK");
    }

    @Override
    public @Nullable String suggestHomePath() {
        return File.separator + "dev" + File.separator + "sdk" + File.separator + "kscr";
    }

    @Override
    public boolean isValidSdkHome(@NotNull String path) {
        File f = new File(path);
        File[] array = f.listFiles();
        if (array == null)
            return false;
        return f.exists() && Arrays.stream(array).anyMatch(x -> x.getName().equals("kscr.exe"));
    }

    @Override
    public @NotNull String suggestSdkName(@Nullable String currentSdkName, @NotNull String sdkHome) {
        return currentSdkName == null ? getName() : currentSdkName;
    }

    @Override
    public @NotNull @Nls(capitalization = Nls.Capitalization.Title) String getPresentableName() {
        return getName();
    }

    @Override
    public @Nullable AdditionalDataConfigurable createAdditionalDataConfigurable(@NotNull SdkModel sdkModel, @NotNull SdkModificator sdkModificator) {
        return null;
    }

    @Override
    public void saveAdditionalData(@NotNull SdkAdditionalData additionalData, @NotNull Element additional) {
    }
}
