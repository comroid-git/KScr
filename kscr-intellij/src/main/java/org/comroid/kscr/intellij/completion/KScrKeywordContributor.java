package org.comroid.kscr.intellij.completion;

import com.intellij.codeInsight.completion.*;
import com.intellij.codeInsight.lookup.LookupElement;
import com.intellij.patterns.PlatformPatterns;
import org.jetbrains.annotations.NotNull;

public class KScrKeywordContributor extends CompletionContributor {
    public KScrKeywordContributor() {
    }

    @Override
    public void fillCompletionVariants(@NotNull CompletionParameters parameters, @NotNull CompletionResultSet result) {
        super.fillCompletionVariants(parameters, result);
    }
}
