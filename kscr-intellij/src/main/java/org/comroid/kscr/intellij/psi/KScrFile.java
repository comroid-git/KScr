package org.comroid.kscr.intellij.psi;

import com.intellij.extapi.psi.PsiFileBase;
import com.intellij.lang.Language;
import com.intellij.openapi.fileTypes.FileType;
import com.intellij.psi.FileViewProvider;
import org.comroid.kscr.intellij.KScrFileType;
import org.comroid.kscr.intellij.KScrLanguage;
import org.jetbrains.annotations.NotNull;

public class KScrFile extends PsiFileBase {
    protected KScrFile(@NotNull FileViewProvider viewProvider) {
        super(viewProvider, KScrLanguage.LANGUAGE);
    }

    @Override
    public @NotNull FileType getFileType() {
        return KScrFileType.SOURCE;
    }
}
