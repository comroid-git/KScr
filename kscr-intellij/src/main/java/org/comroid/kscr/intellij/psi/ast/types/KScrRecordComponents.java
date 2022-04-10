package org.comroid.kscr.intellij.psi.ast.types;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrStubElement;
import org.comroid.kscr.intellij.psi.ast.common.KScrParameter;
import org.comroid.kscr.intellij.psi.stubs.StubKScrRecordComponents;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.List;

public class KScrRecordComponents extends KScrStubElement<KScrRecordComponents, StubKScrRecordComponents>{
	
	public KScrRecordComponents(@NotNull ASTNode node){
		super(node);
	}
	
	public KScrRecordComponents(@NotNull StubKScrRecordComponents components){
		super(components, StubTypes.KSCR_RECORD_COMPONENTS);
	}
	
	public List<KScrParameter> components(){
		return PsiUtils.childrenOfType(this, KScrParameter.class);
	}
}