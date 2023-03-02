package org.comroid.kscr.intellij.parser;

import com.intellij.lang.ASTNode;
import com.intellij.lang.ParserDefinition;
import com.intellij.lang.PsiParser;
import com.intellij.lexer.Lexer;
import com.intellij.openapi.project.Project;
import com.intellij.psi.FileViewProvider;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiFile;
import com.intellij.psi.tree.IFileElementType;
import com.intellij.psi.tree.TokenSet;
import org.antlr.v4.runtime.CommonTokenStream;
import org.antlr.v4.runtime.UnbufferedCharStream;
import org.comroid.kscr.intellij.antlr_generated.KScrLexer;
import org.comroid.kscr.intellij.antlr_generated.KScrParser;
import org.comroid.kscr.intellij.psi.Tokens;
import org.jetbrains.annotations.NotNull;

import java.io.ByteArrayInputStream;
import java.io.IOException;

public class KScrParserDefinition implements ParserDefinition {
    private KScrLexer lexer(Project project) {
        try {
            if (project.getWorkspaceFile() == null)
                throw new NullPointerException("Need a file to parse");
            return new KScrLexer(new UnbufferedCharStream(new ByteArrayInputStream(project.getWorkspaceFile().contentsToByteArray())));
        } catch (IOException e) {
            throw new RuntimeException("Could not parse file " + project.getWorkspaceFile(), e);
        }
    }

    @Override
    public @NotNull Lexer createLexer(Project project) {
        return new LexerAdapter(lexer(project));
    }

    @Override
    public @NotNull PsiParser createParser(Project project) {
        if (project.getWorkspaceFile() == null)
            throw new NullPointerException("Need a file to parse");
        return new ParserAdapter(new KScrParser(new CommonTokenStream(lexer(project))));
    }

    @Override
    public @NotNull IFileElementType getFileNodeType() {
        return null;
    }

    @Override
    public @NotNull TokenSet getCommentTokens() {
        return Tokens.Comment;
    }

    @Override
    public @NotNull TokenSet getStringLiteralElements() {
        return Tokens.StringLit;
    }

    @Override
    public @NotNull PsiElement createElement(ASTNode node) {
        return null;
    }

    @Override
    public @NotNull PsiFile createFile(@NotNull FileViewProvider viewProvider) {
        return null;
    }
}
