package org.comroid.kscr.intellij.psi.stubs;

import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.stubs.*;
import org.comroid.kscr.intellij.KScrLanguage;
import org.comroid.kscr.intellij.antlr_generated.KScrParser;
import org.comroid.kscr.intellij.psi.Tokens;
import org.comroid.kscr.intellij.psi.ast.KScrMethod;
import org.comroid.kscr.intellij.psi.ast.KScrModifierList;
import org.comroid.kscr.intellij.psi.ast.KScrParametersList;
import org.comroid.kscr.intellij.psi.ast.KScrTypeRef;
import org.comroid.kscr.intellij.psi.ast.common.KScrParameter;
import org.comroid.kscr.intellij.psi.ast.common.KScrVariableDef;
import org.comroid.kscr.intellij.psi.ast.types.*;
import org.comroid.kscr.intellij.psi.indexes.StubIndexes;
import org.comroid.kscr.intellij.psi.stubs.impl.*;
import org.comroid.kscr.intellij.psi.types.KScrKind;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.function.Function;

public interface StubTypes{

    IStubElementType<StubKScrType, KScrType> KScr_TYPE = new KScrTypeStubElementType();
    IStubElementType<StubKScrMemberWrapper, KScrMemberWrapper> KScr_MEMBER = new KScrMemberStubElementType();
    IStubElementType<StubKScrRecordComponents, KScrRecordComponents> KScr_RECORD_COMPONENTS = new KScrRecordComponentsStubElementType();
    IStubElementType<StubKScrParameter, KScrParameter> KScr_PARAMETER = new KScrParameterStubElementType();
    IStubElementType<StubKScrModifierList, KScrModifierList> KScr_MODIFIER_LIST = new KScrModifierListStubElementType();
    IStubElementType<StubKScrMethod, KScrMethod> KScr_METHOD = new KScrMethodStubElementType();
    IStubElementType<StubKScrField, KScrVariableDef> KScr_FIELD = new KScrFieldStubElementType();

    IStubElementType<StubKScrClassList<KScrExtendsClause>, KScrExtendsClause> KScr_EXTENDS_LIST
            = new KScrClassListStubElementType<>("KScr_EXTENDS_LIST", KScrExtendsClause::new);
    IStubElementType<StubKScrClassList<KScrImplementsClause>, KScrImplementsClause> KScr_IMPLEMENTS_LIST
            = new KScrClassListStubElementType<>("KScr_IMPLEMENTS_LIST", KScrImplementsClause::new);
    IStubElementType<StubKScrClassList<KScrPermitsClause>, KScrPermitsClause> KScr_PERMITS_LIST
            = new KScrClassListStubElementType<>("KScr_PERMITS_LIST", KScrPermitsClause::new);

    IStubElementType<EmptyStub<?>, KScrParametersList> KScr_PARAMETERS_LIST
            = new EmptyStubElementType<>("KScr_PARAMETERS_LIST", KScrLanguage.LANGUAGE){
        @SuppressWarnings("unchecked")
        public KScrParametersList createPsi(@NotNull EmptyStub<?> stub){
            return new KScrParametersList((EmptyStub<KScrParametersList>)stub);
        }
        public @NotNull String getExternalId(){
            return "kScr." + this;
        }
    };

    class KScrTypeStubElementType extends IStubElementType<StubKScrType, KScrType>{

        public KScrTypeStubElementType(){
            super("KScr_TYPE", KScrLanguage.LANGUAGE);
        }

        public KScrType createPsi(@NotNull StubKScrType stub){
            return new KScrType(stub);
        }

        public @NotNull StubKScrType createStub(@NotNull KScrType psi, StubElement<? extends PsiElement> parent){
            var stub = psi.getStub();
            return stub != null ? stub : new StubImplKScrType(parent, psi);
        }

        public @NotNull String getExternalId(){
            return "kScr." + this;
        }

        public void serialize(@NotNull StubKScrType stub, @NotNull StubOutputStream stream) throws IOException{
            stream.writeName(stub.shortName());
            stream.writeName(stub.fullyQualifiedName());
            stream.writeVarInt(stub.kind().ordinal());
        }

        public @NotNull StubKScrType deserialize(@NotNull StubInputStream stream, StubElement parent) throws IOException{
            return new StubImplKScrType(parent,
                    stream.readNameString(),
                    stream.readNameString(),
                    KScrKind.values()[stream.readVarInt()]);
        }

        public void indexStub(@NotNull StubKScrType stub, @NotNull IndexSink sink){
            sink.occurrence(StubIndexes.TYPES_BY_FQ_NAME, stub.fullyQualifiedName());
            sink.occurrence(StubIndexes.TYPES_BY_SHORT_NAME, stub.shortName());
        }
    }

    class KScrClassListStubElementType<CL extends KScrClassList<CL>> extends IStubElementType<StubKScrClassList<CL>, CL>{

        private final Function<StubKScrClassList<CL>, CL> builder;

        public KScrClassListStubElementType(String name, Function<StubKScrClassList<CL>, CL> builder){
            super(name, KScrLanguage.LANGUAGE);
            this.builder = builder;
        }

        public CL createPsi(@NotNull StubKScrClassList<CL> stub){
            return builder.apply(stub);
        }

        public @NotNull StubKScrClassList<CL> createStub(@NotNull CL psi, StubElement<? extends PsiElement> parent){
            var stub = psi.getStub();
            return stub != null ? stub : new StubImplKScrClassList<>(parent, this, psi.elementNames());
        }

        public @NotNull String getExternalId(){
            return "kScr." + this;
        }

        public void serialize(@NotNull StubKScrClassList<CL> stub, @NotNull StubOutputStream stream) throws IOException{
            var names = stub.elementFqNames();
            stream.writeVarInt(names.size());
            for(String name : names)
                stream.writeName(name);
        }

        public @NotNull StubKScrClassList<CL> deserialize(@NotNull StubInputStream stream, StubElement parent) throws IOException{
            int size = stream.readVarInt();
            var list = new ArrayList<String>(size);
            for(int i = 0; i < size; i++)
                list.add(stream.readNameString());
            return new StubImplKScrClassList<>(parent, this, list);
        }

        @SuppressWarnings("EqualsBetweenInconvertibleTypes")
        public void indexStub(@NotNull StubKScrClassList<CL> stub, @NotNull IndexSink sink){
            if(this.equals(KScr_EXTENDS_LIST) || this.equals(KScr_IMPLEMENTS_LIST))
                for(String name : stub.elementFqNames())
                    sink.occurrence(StubIndexes.INHERITANCE_LISTS, name);
        }
    }

    class KScrMemberStubElementType extends IStubElementType<StubKScrMemberWrapper, KScrMemberWrapper>{

        public KScrMemberStubElementType(){
            super("KScr_MEMBER", KScrLanguage.LANGUAGE);
        }

        public KScrMemberWrapper createPsi(@NotNull StubKScrMemberWrapper stub){
            return new KScrMemberWrapper(stub);
        }

        public @NotNull StubKScrMemberWrapper createStub(@NotNull KScrMemberWrapper psi, StubElement<? extends PsiElement> parent){
            return new StubImplKScrMemberWrapper(parent);
        }

        public @NotNull String getExternalId(){
            return "kScr." + this;
        }

        public void serialize(@NotNull StubKScrMemberWrapper stub, @NotNull StubOutputStream dataStream){}

        public @NotNull StubKScrMemberWrapper deserialize(@NotNull StubInputStream dataStream, StubElement parent){
            return new StubImplKScrMemberWrapper(parent);
        }

        public void indexStub(@NotNull StubKScrMemberWrapper stub, @NotNull IndexSink sink){}
    }

    class KScrRecordComponentsStubElementType extends IStubElementType<StubKScrRecordComponents, KScrRecordComponents>{

        public KScrRecordComponentsStubElementType(){
            super("KScr_RECORD_COMPONENTS", KScrLanguage.LANGUAGE);
        }

        public KScrRecordComponents createPsi(@NotNull StubKScrRecordComponents stub){
            return new KScrRecordComponents(stub);
        }

        public @NotNull StubKScrRecordComponents createStub(@NotNull KScrRecordComponents psi, StubElement<? extends PsiElement> parent){
            return new StubImplKScrRecordComponents(parent);
        }

        public @NotNull String getExternalId(){
            return "kScr." + this;
        }

        public void serialize(@NotNull StubKScrRecordComponents stub, @NotNull StubOutputStream stream){}

        public @NotNull StubKScrRecordComponents deserialize(@NotNull StubInputStream stream, StubElement parentStub){
            return new StubImplKScrRecordComponents(parentStub);
        }

        public void indexStub(@NotNull StubKScrRecordComponents stub, @NotNull IndexSink sink){}
    }

    class KScrParameterStubElementType extends IStubElementType<StubKScrParameter, KScrParameter>{

        public KScrParameterStubElementType(){
            super("KScr_PARAMETER", KScrLanguage.LANGUAGE);
        }

        public KScrParameter createPsi(@NotNull StubKScrParameter stub){
            return new KScrParameter(stub);
        }

        public @NotNull StubKScrParameter createStub(@NotNull KScrParameter psi, StubElement<? extends PsiElement> parent){
            String name = psi.varName(), type = psi.getTypeName().map(PsiElement::getText).orElse("");
            boolean varargs = psi.isVarargs();
            return new StubImplKScrParameter(parent, name, type, varargs);
        }

        public @NotNull String getExternalId(){
            return "kScr." + this;
        }

        public void serialize(@NotNull StubKScrParameter stub, @NotNull StubOutputStream stream) throws IOException{
            stream.writeName(stub.name());
            stream.writeName(stub.typeText());
            stream.writeBoolean(stub.isVarargs());
        }

        public @NotNull StubKScrParameter deserialize(@NotNull StubInputStream stream, StubElement parent) throws IOException{
            var name = stream.readNameString();
            var type = stream.readNameString();
            return new StubImplKScrParameter(parent, name != null ? name : "", type != null ? type : "", stream.readBoolean());
        }

        public void indexStub(@NotNull StubKScrParameter stub, @NotNull IndexSink sink){
            if(stub.isRecordComponent())
                sink.occurrence(StubIndexes.FIELDS, stub.name());
        }
    }

    class KScrModifierListStubElementType extends IStubElementType<StubKScrModifierList, KScrModifierList>{

        public KScrModifierListStubElementType(){
            super("KScr_MODIFIER_LIST", KScrLanguage.LANGUAGE);
        }

        public KScrModifierList createPsi(@NotNull StubKScrModifierList stub){
            return new KScrModifierList(stub);
        }

        public @NotNull StubKScrModifierList createStub(@NotNull KScrModifierList psi, StubElement<? extends PsiElement> parent){
            return new StubImplKScrModifierList(parent, Collections.unmodifiableList(psi.getModifiers()));
        }

        public @NotNull String getExternalId(){
            return "kScr." + this;
        }

        public void serialize(@NotNull StubKScrModifierList stub, @NotNull StubOutputStream stream) throws IOException{
            var list = stub.modifiers();
            stream.writeVarInt(list.size());
            for(String mod : list)
                stream.writeName(mod);
        }

        public @NotNull StubKScrModifierList deserialize(@NotNull StubInputStream stream, StubElement parent) throws IOException{
            int amount = stream.readVarInt();
            List<String> modifiers = new ArrayList<>(amount);
            for(int i = 0; i < amount; i++)
                modifiers.add(stream.readNameString());
            return new StubImplKScrModifierList(parent, modifiers);
        }

        public void indexStub(@NotNull StubKScrModifierList stub, @NotNull IndexSink sink){}
    }

    class KScrMethodStubElementType extends IStubElementType<StubKScrMethod, KScrMethod>{

        public KScrMethodStubElementType(){
            super("KScr_METHOD", KScrLanguage.LANGUAGE);
        }

        public KScrMethod createPsi(@NotNull StubKScrMethod stub){
            return new KScrMethod(stub);
        }

        public @NotNull StubKScrMethod createStub(@NotNull KScrMethod psi, StubElement<? extends PsiElement> parent){
            return new StubImplKScrMethod(parent, psi.getName(), psi.returns().map(PsiElement::getText).orElse(""), psi.hasSemicolon());
        }

        public @NotNull String getExternalId(){
            return "kScr." + this;
        }

        public void serialize(@NotNull StubKScrMethod stub, @NotNull StubOutputStream stream) throws IOException{
            stream.writeName(stub.name());
            stream.writeName(stub.returnTypeText());
            stream.writeBoolean(stub.hasSemicolon());
        }

        public @NotNull StubKScrMethod deserialize(@NotNull StubInputStream stream, StubElement parent) throws IOException{
            return new StubImplKScrMethod(parent, stream.readNameString(), stream.readNameString(), stream.readBoolean());
        }

        public void indexStub(@NotNull StubKScrMethod stub, @NotNull IndexSink sink){
            sink.occurrence(StubIndexes.METHODS, stub.name());
        }
    }

    class KScrFieldStubElementType extends IStubElementType<StubKScrField, KScrVariableDef>{

        public KScrFieldStubElementType(){
            super("KScr_FIELD", KScrLanguage.LANGUAGE);
        }

        public KScrVariableDef createPsi(@NotNull StubKScrField stub){
            return new KScrVariableDef(stub);
        }

        public @NotNull StubKScrField createStub(@NotNull KScrVariableDef psi, StubElement<? extends PsiElement> parent){
            return new StubImplKScrField(
                    parent,
                    psi.varName(),
                    PsiUtils.childOfType(psi, KScrTypeRef.class).map(PsiElement::getText).orElse(""));
        }

        public @NotNull String getExternalId(){
            return "kScr." + this;
        }

        public void serialize(@NotNull StubKScrField stub, @NotNull StubOutputStream stream) throws IOException{
            stream.writeName(stub.name());
            stream.writeName(stub.typeText());
        }

        public @NotNull StubKScrField deserialize(@NotNull StubInputStream stream, StubElement parent) throws IOException{
            var name = stream.readNameString();
            var type = stream.readNameString();
            return new StubImplKScrField(parent, name != null ? name : "", type != null ? type : "");
        }

        public void indexStub(@NotNull StubKScrField stub, @NotNull IndexSink sink){
            sink.occurrence(StubIndexes.FIELDS, stub.name());
        }

        public boolean shouldCreateStub(ASTNode node){
            // only fields, not locals
            return node.getTreeParent().getElementType() == Tokens.getRuleFor(KScrParser.RULE_member);
        }
    }
}