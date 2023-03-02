package org.comroid.kscr.intellij.completion;

import com.intellij.codeInsight.completion.CompletionContributor;
import com.intellij.codeInsight.completion.CompletionParameters;
import com.intellij.codeInsight.completion.CompletionResultSet;
import com.intellij.codeInsight.lookup.LookupElement;
import org.comroid.kscr.intellij.antlr_generated.KScrLexer;
import org.comroid.kscr.intellij.psi.Tokens;
import org.jetbrains.annotations.NotNull;

import java.util.Arrays;
import java.util.Objects;
import java.util.stream.IntStream;
import java.util.stream.Stream;

public class KScrKeywordContributor extends CompletionContributor {
    @Override
    public void fillCompletionVariants(@NotNull CompletionParameters parameters, @NotNull CompletionResultSet result) {
        Stream<? extends Element> contextResults = Stream.empty();
        Stream.concat(
                IntStream.concat(IntStream.concat(
                                Arrays.stream(Tokens.LiteralsTokens),
                                Arrays.stream(Tokens.PrimitiveTypeTokens)
                        ), Arrays.stream(Tokens.InfrastructureTokens))
                        .mapToObj(KScrLexer.VOCABULARY::getLiteralName)
                        .filter(Objects::nonNull)
                        .map(str -> str.substring(1, str.length() - 1))
                        .map(Element::new),
                        contextResults)
                .forEachOrdered(result::addElement);
        super.fillCompletionVariants(parameters, result);
    }

    private static class Element extends LookupElement {
        private final String str;

        private Element(String str) {
            this.str = str;
        }

        @Override
        public @NotNull String getLookupString() {
            return str;
        }
    }
}
