package org.comroid.kscr.intellij.psi.ast.types;

import com.intellij.icons.AllIcons;
import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmClass;
import com.intellij.lang.jvm.JvmMethod;
import com.intellij.openapi.project.DumbService;
import com.intellij.psi.CommonClassNames;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiFile;
import com.intellij.psi.impl.source.tree.SharedImplUtil;
import com.intellij.psi.stubs.IStubElementType;
import com.intellij.psi.stubs.StubElement;
import com.intellij.ui.LayeredIcon;
import com.intellij.util.IncorrectOperationException;
import com.intellij.util.PlatformIcons;
import com.intellij.util.ui.EDT;
import org.comroid.kscr.intellij.KScrIcons;
import org.comroid.kscr.intellij.psi.*;
import org.comroid.kscr.intellij.psi.ast.KScrFileWrapper;
import org.comroid.kscr.intellij.psi.ast.KScrMethod;
import org.comroid.kscr.intellij.psi.ast.KScrPackageStatement;
import org.comroid.kscr.intellij.psi.ast.common.KScrParameter;
import org.comroid.kscr.intellij.psi.ast.common.KScrVariableDef;
import org.comroid.kscr.intellij.psi.stubs.*;
import org.comroid.kscr.intellij.psi.types.KScrKind;
import org.comroid.kscr.intellij.psi.utils.KScrModifiersHolder;
import org.comroid.kscr.intellij.psi.utils.ProjectTypeFinder;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.Optional;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class KScrType extends KScrDefinitionStubElement<KScrType, StubKScrType> implements KScrModifiersHolder {
	
	public KScrType(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrType(@NotNull StubKScrType stub){
		super(stub, StubTypes.KSCR_TYPE);
	}
	
	public boolean isTopLevelType(){
		return getParent() instanceof KScrFileWrapper;
	}
	
	public String getPackageName(){
		if(getContainingFile() instanceof KScrFile){
			KScrFile file = (KScrFile)getContainingFile();
			return file.getPackage().map(KScrPackageStatement::getPackageName).orElse("");
		}
		return "";
	}
	
	public @NotNull String fullyQualifiedName(){
		var stub = getStub();
		if(stub != null)
			return stub.fullyQualifiedName();
		
		// avoids PsiInvalidElementAccessException by skipping the stub-based implementation that doesn't work here anyways
		PsiFile file = SharedImplUtil.getContainingFile(getNode());
		if(file instanceof KScrFile)
			return ((KScrFile)file).getPackage().map(k -> k.getPackageName() + ".").orElse("") + super.fullyQualifiedName();
		return super.fullyQualifiedName();
	}
	
	public @NotNull KScrKind kind(){
		var stub = getStub();
		if(stub != null)
			return stub.kind();
		
		var objType = getNode().findChildByType(Tokens.getRuleFor(KScrlicLangParser.RULE_objectType));
		if(objType != null){
			if(objType.findChildByType(Tokens.TOK_CLASS) != null)
				return KScrKind.CLASS;
			if(objType.findChildByType(Tokens.TOK_INTERFACE) != null)
				return KScrKind.INTERFACE;
			if(objType.findChildByType(Tokens.TOK_ANNOTATION) != null || objType.findChildByType(Tokens.TOK_AT) != null)
				return KScrKind.ANNOTATION;
			if(objType.findChildByType(Tokens.TOK_ENUM) != null)
				return KScrKind.ENUM;
			if(objType.findChildByType(Tokens.TOK_RECORD) != null)
				return KScrKind.RECORD;
			if(objType.findChildByType(Tokens.TOK_SINGLE) != null)
				return KScrKind.SINGLE;
		}
		return KScrKind.CLASS;
	}
	
	public boolean isFinal(){
		return hasModifier("final");
	}
	
	@SuppressWarnings("unchecked")
	public @NotNull List<? extends JvmMethod> declaredMethods(){
		var stub = getStub();
		if(stub != null){
			ArrayList<JvmMethod> methods = stub.getChildrenStubs().stream()
					.filter(StubKScrMemberWrapper.class::isInstance)
					.flatMap(x -> (Stream<StubElement<?>>)x.getChildrenStubs().stream())
					.filter(StubKScrMethod.class::isInstance)
					.map(StubKScrMethod.class::cast)
					.map(StubElement::getPsi)
					.map(JvmKScrlicMethod::of)
					.collect(Collectors.toCollection(ArrayList::new));
			var recComponents = stub.findChildStubByType(StubTypes.KSCR_RECORD_COMPONENTS);
			if(recComponents != null){
				var comps = recComponents.components();
				for(StubKScrParameter comp : comps)
					if(methods.stream().noneMatch(m -> m.getParameters().length == 0 && Objects.equals(m.getName(), comp.name())))
						methods.add(AsPsiUtil.recordAccessorMethod(comp.getPsi()));
			}
			return methods;
		}
		
		List<JvmMethod> methods
				= PsiUtils.wrappedChildrenOfType(this, KScrMethod.class).stream().map(JvmKScrlicMethod::of).collect(Collectors.toList());
		List<KScrParameter> components = PsiUtils.childOfType(this, KScrRecordComponents.class)
				.map(KScrRecordComponents::components)
				.orElse(List.of());
		for(KScrParameter comp : components)
			if(methods.stream().noneMatch(m -> m.getParameters().length == 0 && Objects.equals(m.getName(), comp.varName())))
				methods.add(AsPsiUtil.recordAccessorMethod(comp));
		return methods;
	}
	
	public @NotNull List<? extends KScrVariable> fields(){
		List<KScrVariable> defs = new ArrayList<>(PsiUtils.wrappedChildrenOfType(this, KScrVariableDef.class));
		PsiUtils.childOfType(this, KScrRecordComponents.class).ifPresent(rc -> defs.addAll(rc.components()));
		return defs;
	}
	
	public PsiElement setName(@NotNull String name) throws IncorrectOperationException{
		// also change the file name if top level
		if(isTopLevelType())
			getContainingFile().setName(name + ".kscr");
		return super.setName(name);
	}
	
	public List<KScrMemberWrapper> getMembers(){
		return PsiUtils.childrenOfType(this, KScrMemberWrapper.class);
	}
	
	public @NotNull String name(){
		return getName();
	}
	
	public @Nullable Icon getIcon(int flags){
		var result = KScrlicIcons.KScr_FILE;
		var objType = getNode().findChildByType(Tokens.getRuleFor(KScrlicLangParser.RULE_objectType));
		if(objType != null){
			if(objType.findChildByType(Tokens.TOK_CLASS) != null)
				result = PlatformIcons.CLASS_ICON;
			else if(objType.findChildByType(Tokens.TOK_INTERFACE) != null)
				result = PlatformIcons.INTERFACE_ICON;
			else if(objType.findChildByType(Tokens.TOK_ANNOTATION) != null || objType.findChildByType(Tokens.TOK_AT) != null)
				result = PlatformIcons.ANNOTATION_TYPE_ICON;
			else if(objType.findChildByType(Tokens.TOK_ENUM) != null)
				result = PlatformIcons.ENUM_ICON;
			else if(objType.findChildByType(Tokens.TOK_RECORD) != null)
				result = PlatformIcons.RECORD_ICON;
			else if(objType.findChildByType(Tokens.TOK_SINGLE) != null)
				result = AllIcons.Nodes.Static;
		}
		if(!DumbService.isDumb(getProject())
				&& !EDT.isCurrentThreadEdt() // TODO: can this can be refactored to not require slow operations?
				&& JvmClassUtils.hasMainMethod(JvmKScrlicClass.of(this)))
			result = new LayeredIcon(result, AllIcons.Nodes.RunnableMark);
		return new LayeredIcon(result, KScrlicIcons.KScr_DECORATION);
	}
	
	@Nullable
	public JvmClass getSuperType(){
		if(kind() == KScrKind.INTERFACE)
			return null;
		if(kind() == KScrKind.RECORD)
			return JvmClassUtils.classByName(CommonClassNames.JAVA_LANG_RECORD, getProject());
		if(kind() == KScrKind.ENUM)
			return JvmClassUtils.classByName(CommonClassNames.JAVA_LANG_ENUM, getProject());
		
		var object = JvmClassUtils.classByName(CommonClassNames.JAVA_LANG_OBJECT, getProject());
		
		var stub = getStub();
		if(stub != null){
			var extList = stub.extendsList();
			if(extList != null){
				var names = extList.elementFqNames();
				if(names.size() > 0)
					return ProjectTypeFinder.getByName(getProject(), names.get(0), this);
				else
					return object;
			}
		}
		
		var exts = PsiUtils.childOfType(this, KScrExtendsClause.class);
		return exts.flatMap(KScrClassList::first).orElse(object);
	}
	
	@NotNull
	public List<JvmClass> getInterfaces(){
		var stub = getStub();
		if(stub != null){
			var cl = (kind() == KScrKind.INTERFACE) ? stub.extendsList() : stub.implementsList();
			if(cl != null){
				var names = cl.elementFqNames();
				return names.stream()
						.map(x -> ProjectTypeFinder.getByName(getProject(), x, this))
						.collect(Collectors.toList());
			}
		}
		
		Optional<? extends KScrClassList<?>> list;
		if(kind() == KScrKind.INTERFACE)
			list = PsiUtils.childOfType(this, KScrExtendsClause.class);
		else
			list = PsiUtils.childOfType(this, KScrImplementsClause.class);
		return list.map(KScrClassList::elements).orElse(List.of());
	}
	
	public IStubElementType<StubKScrType, KScrType> getElementType(){
		return StubTypes.KSCR_TYPE;
	}
}