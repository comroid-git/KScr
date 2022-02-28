package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.List;

public class KScrRecordComponents extends KScrElement{
	
	public KScrRecordComponents(@NotNull ASTNode node){
		super(node);
	}
	
	public List<KScrParameter> components(){
		return PsiUtils.childrenOfType(this, KScrParameter.class);
	}
}