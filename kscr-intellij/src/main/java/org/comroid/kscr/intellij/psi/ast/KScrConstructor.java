package org.comroid.kscr.intellij.psi.ast;

import com.intellij.lang.ASTNode;
import com.intellij.lang.jvm.JvmMethod;
import com.intellij.psi.util.PsiTreeUtil;
import org.comroid.kscr.intellij.psi.KScrCodeHolder;
import org.comroid.kscr.intellij.psi.KScrDefinitionAstElement;
import org.comroid.kscr.intellij.psi.KScrModifiersHolder;
import org.comroid.kscr.intellij.psi.ast.common.KScrParameter;
import org.comroid.kscr.intellij.psi.ast.statements.KScrStatement;
import org.comroid.kscr.intellij.psi.ast.types.KScrType;
import org.comroid.kscr.intellij.psi.types.JvmKScrlicConstructor;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.List;
import java.util.Optional;

// TODO: stubs
public class KScrConstructor extends KScrDefinitionAstElement implements KScrModifiersHolder, KScrCodeHolder{
	
	public KScrConstructor(@NotNull ASTNode node){
		super(node);
	}
	
	public List<KScrParameter> parameters(){
		var paramList = PsiUtils.childOfType(this, KScrParametersList.class);
		return paramList.map(list -> PsiUtils.childrenOfType(list, KScrParameter.class)).orElseGet(List::of);
	}
	
	public Optional<KScrStatement> body(){
		// last child is either a block or (-> +) statement
		var last = getLastChild();
		if(last instanceof KScrStatement)
			return Optional.of((KScrStatement)last);
		return Optional.empty();
	}
	
	public JvmMethod toJvm(){
		return JvmKScrlicConstructor.of(this);
	}
	
	public KScrType containingType(){
		return PsiTreeUtil.getParentOfType(this, KScrType.class);
	}
}
