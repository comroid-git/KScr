package org.comroid.kscr.intellij.facet;

import com.intellij.ide.util.frameworkSupport.FrameworkSupportConfigurable;
import com.intellij.ide.util.frameworkSupport.FrameworkSupportModel;
import com.intellij.ide.util.frameworkSupport.FrameworkSupportProvider;
import com.intellij.openapi.module.ModuleType;
import org.jetbrains.annotations.Nls;
import org.jetbrains.annotations.NonNls;
import org.jetbrains.annotations.NotNull;

public class KScrFrameworkSupport extends FrameworkSupportProvider {
    protected KScrFrameworkSupport() {
        super("kscr-framework", "KScr Framework Support");
    }

    @Override
    public @NotNull FrameworkSupportConfigurable createConfigurable(@NotNull FrameworkSupportModel model) {
        return null;//Todo
    }

    @Override
    public boolean isEnabledForModuleType(@NotNull ModuleType moduleType) {
        return false;
    }
}
