package org.comroid.kscr.intellij.psi.utils;

import com.intellij.psi.PsiElement;
import org.comroid.kscr.intellij.psi.*;
import org.comroid.kscr.intellij.psi.expressions.KScrBinaryExpr;
import org.comroid.kscr.intellij.psi.expressions.KScrParenthesisedExpr;

import java.util.ArrayList;
import java.util.List;

public class MethodUtils{
	
	public static List<KScrExpression> getRealArgs(KScrCall call){
		var declaredArgs = PsiUtils.childOfType(call, KScrArgumentsList.class)
				.map(x -> PsiUtils.childrenOfType(x, KScrExpression.class))
				.orElse(List.of());
		if(call.getParent() instanceof KScrStatement)
			return declaredArgs;
		
		var args = new ArrayList<>(declaredArgs);
		PsiElement current = call.getParent();
		while(current instanceof KScrExpression){
			current = current.getParent();
			if(!(current instanceof KScrParenthesisedExpr || current instanceof KScrBinaryExpr))
				break;
			if(current instanceof KScrBinaryExpr){
				var bin = (KScrBinaryExpr)current;
				var symbol = bin.symbol();
				if(!symbol.equals("|>"))
					break;
				bin.left().ifPresent(x -> args.add(0, x));
			}
		}
		return args;
	}
}