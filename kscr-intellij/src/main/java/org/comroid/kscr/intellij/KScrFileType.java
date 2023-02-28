package org.comroid.kscr.intellij;

import com.intellij.openapi.fileTypes.LanguageFileType;
import com.intellij.openapi.util.NlsContexts;
import com.intellij.openapi.util.NlsSafe;
import org.jetbrains.annotations.NonNls;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;

public final class KScrFileType extends LanguageFileType {
    public static final KScrFileType SOURCE = new KScrFileType("KScr Source File", "Provides KScr source code", "kscr", Icons.KSCR, false);
    public static final KScrFileType BINARY = new KScrFileType("KScr Binary File", "Contains KScr Bytecode", "kbin", Icons.KSCR, true);
    public static final KScrFileType MODULE = new KScrFileType("KScr Module File", "Contains a KScr library or application", "kmod", Icons.KSCR, true);
    public static final KScrFileType MODULE_DESC = new KScrFileType("KScr Module description File", "Provides build info", "kmod.json", Icons.KSCR, true);
    private final String name;
    private final String description;
    private final String extension;
    private final Icon icon;

    private KScrFileType(String name, String description, String extension, Icon icon, boolean secondary) {
        super(KScrLanguage.LANGUAGE, secondary);
        this.name = name;
        this.description = description;
        this.extension = extension;
        this.icon = icon;
    }

    @Override
    public @NonNls @NotNull String getName() {
        return name;
    }

    @Override
    public @NlsContexts.Label @NotNull String getDescription() {
        return description;
    }

    @Override
    public @NlsSafe @NotNull String getDefaultExtension() {
        return extension;
    }

    @Override
    public @Nullable Icon getIcon() {
        return icon;
    }
}
