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
import com.intellij.psi.tree.IStubFileElementType;
import com.intellij.psi.tree.TokenSet;
import org.comroid.kscr.intellij.KScrLanguage;
import org.comroid.kscr.intellij.antlr_generated.KScrLexer;
import org.comroid.kscr.intellij.antlr_generated.KScrParser;
import org.comroid.kscr.intellij.psi.Tokens;
import org.comroid.kscr.intellij.psi.ast.statements.*;
import org.comroid.kscr.intellij.psi.ast.common.*;
import org.comroid.kscr.intellij.psi.ast.types.*;
import org.jetbrains.annotations.NonNls;
import org.jetbrains.annotations.NotNull;

public class KScrParserDefinition implements ParserDefinition {
    public static final IStubFileElementType<KScrFileStub> FILE =
            new IStubFileElementType<>("cyclic.FILE", KScrLanguage.LANGUAGE){
                public @NonNls @NotNull String getExternalId(){
                    return "cyclic.FILE";
                }
            };

    @Override
    public @NotNull Lexer createLexer(Project project) {
        return new LexerAdapter(new KScrLexer(null));
    }

    @Override
    public @NotNull PsiParser createParser(Project project) {
        return new ParserAdapter();
    }

    @Override
    public @NotNull IFileElementType getFileNodeType() {
        return FILE;
    }

    @Override
    public @NotNull TokenSet getCommentTokens() {
        return Tokens.COMMENTS;
    }

    @Override
    public @NotNull TokenSet getWhitespaceTokens() {
        return Tokens.WHITESPACES;
    }

    @Override
    public @NotNull TokenSet getStringLiteralElements() {
        return Tokens.STRING_LITERALS;
    }

    @Override
    public @NotNull PsiElement createElement(ASTNode node) {
        // TODO: replace with switch (by rule index)
        var type = node.getElementType();
        if(type == Tokens.getRuleFor(KScrParser.RULE_file))
            return new KScrFileWrapper(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_classDecl))
            return new KScrType(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_id))
            return new KScrId(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_idPart))
            return new KScrIdPart(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_packageDecl))
            return new KScrPackageStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_importDecl))
            return new KScrImportStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_imports))
            return new KScrImportList(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_annotation))
            return new KScrAnnotation(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_rawType))
            return new KScrRawTypeRef(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_type))
            return new KScrTypeRef(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_typeOrInferred))
            return new KScrTypeRefOrInferred(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_member))
            return new KScrMemberWrapper(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_modifiers))
            return new KScrModifierList(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_modifier))
            return new KScrModifier(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_function))
            return new KScrMethod(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_constructor))
            return new KScrConstructor(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_statement))
            return new KScrStatementWrapper(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_value))
            return createExpr(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_call))
            return new KScrCall(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_initialisation))
            return new KScrInitialisation(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_varAssignment))
            return new KScrVariableAssignment(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_objectExtends))
            return new KScrExtendsClause(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_objectImplements))
            return new KScrImplementsClause(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_objectPermits))
            return new KScrPermitsClause(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_block))
            return new KScrBlock(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_varDecl))
            return new KScrVariableDef(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_parameter))
            return new KScrParameter(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_recordComponents))
            return new KScrRecordComponents(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_arguments))
            return new KScrArgumentsList(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_parameters))
            return new KScrParametersList(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_binaryop))
            return new KScrBinaryOp(node);

        if(type == Tokens.getRuleFor(KScrParser.RULE_assertStatement))
            return new KScrAssertStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_ctorCall))
            return new KScrConstructorCallStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_doWhileStatement))
            return new KScrDoWhileStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_foreachStatement))
            return new KScrForeachStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_forStatement))
            return new KScrForStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_ifStatement))
            return new KScrIfStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_elseStatement))
            return new KScrElseClause(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_returnStatement))
            return new KScrReturnStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_switchStatement))
            return new KScrSwitchStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_throwStatement))
            return new KScrThrowStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_mutation))
            return new KScrVarAssignStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_varIncrement))
            return new KScrVarIncrementStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_whileStatement))
            return new KScrWhileStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_yieldStatement))
            return new KScrYieldStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_tryStatement))
            return new KScrTryCatchStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_catchBlock))
            return new KScrCatchBlock(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_finallyBlock))
            return new KScrFinallyBlock(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_breakStatement))
            return new KScrBreakStatement(node);
        if(type == Tokens.getRuleFor(KScrParser.RULE_continueStatement))
            return new KScrContinueStatement(node);

        return new KScrAstElement(node);
    }

    @Override
    public @NotNull PsiFile createFile(@NotNull FileViewProvider viewProvider) {
        return null;
    }
}
