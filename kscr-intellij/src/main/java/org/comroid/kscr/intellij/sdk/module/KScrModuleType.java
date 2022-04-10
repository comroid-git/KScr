package org.comroid.kscr.intellij.sdk.module;

import com.intellij.openapi.module.ModuleType;
import com.intellij.util.PlatformIcons;
import org.jetbrains.annotations.Nls;
import org.jetbrains.annotations.NotNull;

import javax.swing.*;

public class KScrModuleType extends ModuleType<KScrModuleBuilder> {
    public static final KScrModuleType INSTANCE = new KScrModuleType();

    protected KScrModuleType() {
        super("KScrModule");
    }

    @NotNull
    @Override
    public KScrModuleBuilder createModuleBuilder() {
        return new KScrModuleBuilder(this);
    }

    @Override
    public @NotNull @Nls(capitalization = Nls.Capitalization.Title) String getName() {
        return "KScr Project";
    }

    @Override
    public @NotNull @Nls(capitalization = Nls.Capitalization.Sentence) String getDescription() {
        return "KScr source project; class library or application";
    }

    @Override
    public @NotNull Icon getNodeIcon(boolean isOpened) {
        return isOpened ? PlatformIcons.LIBRARY_ICON : PlatformIcons.FOLDER_ICON;
    }
}
