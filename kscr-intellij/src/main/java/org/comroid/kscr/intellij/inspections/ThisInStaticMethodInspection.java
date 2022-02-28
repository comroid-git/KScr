package org.comroid.kscr.intellij.inspections;

import com.intellij.codeInspection.LocalInspectionTool;
import com.intellij.codeInspection.ProblemHighlightType;
import com.intellij.codeInspection.ProblemsHolder;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import org.comroid.kscr.intellij.psi.KScrMethod;
import org.comroid.kscr.intellij.psi.expressions.KScrThisExpr;
import org.jetbrains.annotations.NotNull;

public class ThisInStaticMethodInspection extends LocalInspectionTool{
	
	public @NotNull PsiElementVisitor buildVisitor(@NotNull ProblemsHolder holder, boolean isOnTheFly){
		return new PsiElementVisitor(){
			public void visitElement(@NotNull PsiElement element){
				super.visitElement(element);
				if(element instanceof KScrThisExpr){
					var container = PsiTreeUtil.getParentOfType(element, KScrMethod.class);
					if(container != null && container.isStatic() && element.getText().equals("this"))
						holder.registerProblem(element, "'this' can only be used in non-static contexts", ProblemHighlightType.ERROR);
				}
			}
		};
	}
}