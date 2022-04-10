package org.comroid.kscr.intellij.psi.stubs;

import com.intellij.psi.stubs.PsiFileStubImpl;
import com.intellij.psi.tree.IStubFileElementType;
import org.comroid.kscr.intellij.parser.KScrParserDefinition;
import org.comroid.kscr.intellij.psi.KScrFile;
import org.jetbrains.annotations.NotNull;

public class KScrFileStub extends PsiFileStubImpl<KScrFile>{
	
	public KScrFileStub(KScrFile file){
		super(file);
	}
	
	public @NotNull IStubFileElementType<KScrFileStub> getType(){
		return KScrParserDefinition.FILE;
	}
}