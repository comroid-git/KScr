package org.comroid.kscr.intellij.inspections;

import com.intellij.codeInspection.InspectionManager;
import com.intellij.codeInspection.LocalInspectionTool;
import com.intellij.codeInspection.ProblemDescriptor;
import com.intellij.psi.PsiFile;
import org.comroid.kscr.intellij.psi.KScrFile;
import org.comroid.kscr.intellij.psi.KScrType;
import org.jetbrains.annotations.NotNull;

import java.util.ArrayList;
import java.util.List;

public abstract class KScrInspection extends LocalInspectionTool{
	
	public ProblemDescriptor @NotNull [] checkTypeDef(@NotNull KScrType type, @NotNull InspectionManager manager, boolean isOnTheFly){
		return new ProblemDescriptor[0];
	}
	
	public ProblemDescriptor @NotNull [] checkFile(@NotNull PsiFile file, @NotNull InspectionManager manager, boolean isOnTheFly){
		List<ProblemDescriptor> problems = new ArrayList<>();
		if(file instanceof KScrFile){
			KScrFile kScrFile = (KScrFile)file;
			problems.addAll(List.of(kScrFile.getTypeDef().map(l -> checkTypeDef(l, manager, isOnTheFly)).orElse(new ProblemDescriptor[0])));
		}
		return problems.toArray(ProblemDescriptor[]::new);
	}
}