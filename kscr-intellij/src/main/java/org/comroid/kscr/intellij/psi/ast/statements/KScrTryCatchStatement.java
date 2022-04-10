package org.comroid.kscr.intellij.psi.ast.statements;

import com.intellij.lang.ASTNode;
import org.comroid.kscr.intellij.psi.KScrAstElement;
import org.comroid.kscr.intellij.psi.ast.common.KScrBlock;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class KScrTryCatchStatement extends KScrAstElement implements KScrStatement{
	
	public KScrTryCatchStatement(@NotNull ASTNode node){
		super(node);
	}
	
	public Optional<KScrStatement> body(){
		return PsiUtils.childOfType(this, KScrBlock.class).map(x -> x); // lol
	}
	
	public Stream<KScrCatchBlock> streamCatchBlocks(){
		return PsiUtils.streamChildrenOfType(this, KScrCatchBlock.class);
	}
	
	public List<KScrCatchBlock> getCatchBlocks(){
		return PsiUtils.childrenOfType(this, KScrCatchBlock.class);
	}
	
	public Stream<KScrStatement> streamCatchBodies(){
		return streamCatchBlocks()
				.map(KScrCatchBlock::body)
				.flatMap(x -> Stream.ofNullable(x.orElse(null)));
	}
	
	public List<KScrStatement> getCatchBodies(){
		return streamCatchBodies().collect(Collectors.toList());
	}
	
	public Optional<KScrFinallyBlock> finallyBlock(){
		return PsiUtils.childOfType(this, KScrFinallyBlock.class);
	}
}