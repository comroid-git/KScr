package org.comroid.kscr.intellij.psi;

import com.intellij.lang.ASTNode;
import com.intellij.psi.util.PsiTreeUtil;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class KScrPackageStatement extends KScrElement{
	
	public KScrPackageStatement(@NotNull ASTNode node){
		super(node);
	}
	
	@Nullable
	public String getPackageName(){
		KScrId id = PsiTreeUtil.findChildOfType(this, KScrId.class);
		if(id != null)
			return id.getText();
		return null;
	}
	
	// TODO: resolve against packages
}
