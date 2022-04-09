package org.comroid.kscr.intellij.inspections;

import com.intellij.codeHighlighting.HighlightDisplayLevel;
import com.intellij.codeInspection.InspectionManager;
import com.intellij.codeInspection.LocalQuickFix;
import com.intellij.codeInspection.ProblemDescriptor;
import com.intellij.codeInspection.ProblemHighlightType;
import org.comroid.kscr.intellij.inspections.fixes.RenameFileToTypeFix;
import org.comroid.kscr.intellij.inspections.fixes.RenameTypeToFileFix;
import org.jetbrains.annotations.NotNull;

public class WrongFileNameForTypeNameInspection extends KScrInspection{
	
	public ProblemDescriptor @NotNull [] checkTypeDef(@NotNull KScrType type, @NotNull InspectionManager manager, boolean isOnTheFly){
		String typeName = type.getName();
		if(type.isTopLevelType() && type.getNameIdentifier() != null)
			if(type.getNameIdentifier().getTextLength() > 0){
				if(!typeName.equals(type.getContainingFile().getName()))
					return new ProblemDescriptor[]{manager.createProblemDescriptor(type.getNameIdentifier(), "KScr type '" + typeName + "' should be declared in file '" + typeName + ".kScr'", new LocalQuickFix[]{new RenameFileToTypeFix(type, typeName), new RenameTypeToFileFix(type, type.getContainingFile().getName())}, ProblemHighlightType.ERROR, isOnTheFly, false)};
			}else return new ProblemDescriptor[]{manager.createProblemDescriptor(type, "KScr type must have name '" + type.getContainingFile().getName() + "'", new LocalQuickFix[]{}, ProblemHighlightType.GENERIC_ERROR, isOnTheFly, false)};
		return super.checkTypeDef(type, manager, isOnTheFly);
	}
	
	public boolean isEnabledByDefault(){
		return true;
	}
	
	@NotNull
	public HighlightDisplayLevel getDefaultLevel() {
		return HighlightDisplayLevel.ERROR;
	}
	
	@NotNull
	public String getShortName(){
		return "WrongFileNameForTypeName";
	}
}