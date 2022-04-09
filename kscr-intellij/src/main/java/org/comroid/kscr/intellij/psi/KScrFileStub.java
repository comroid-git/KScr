package org.comroid.kscr.intellij.psi;

import com.intellij.psi.stubs.PsiFileStubImpl;
import com.intellij.psi.tree.IStubFileElementType;
import org.comroid.kscr.intellij.parser.KScrParserDefinition;
import org.jetbrains.annotations.NotNull;

public class KScrFileStub extends PsiFileStubImpl<KScrFile> {
    public KScrFileStub(KScrFile file) {
        super(file);
    }

    @Override
    public @NotNull IStubFileElementType<KScrFileStub> getType() {
        return KScrParserDefinition.FILE;
    }
}
