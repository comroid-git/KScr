package org.comroid.kscr.intellij.psi;

import com.intellij.extapi.psi.PsiFileBase;
import com.intellij.openapi.fileTypes.FileType;
import com.intellij.psi.FileViewProvider;
import org.comroid.kscr.intellij.KScrFileType;
import org.comroid.kscr.intellij.KScrLanguage;
import org.comroid.kscr.intellij.KScrSourceFileType;
import org.comroid.kscr.intellij.psi.ast.KScrImportStatement;
import org.comroid.kscr.intellij.psi.ast.KScrPackageStatement;
import org.comroid.kscr.intellij.psi.ast.types.KScrType;
import org.comroid.kscr.intellij.psi.utils.PsiUtils;
import org.jetbrains.annotations.NotNull;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

public class KScrFile extends PsiFileBase{
	
	public KScrFile(@NotNull FileViewProvider viewProvider){
		super(viewProvider, KScrLanguage.LANGUAGE);
	}
	
	public @NotNull FileType getFileType(){
		return KScrSourceFileType.FILE_TYPE;
	}
	
	public Optional<KScrFileWrapper> wrapper(){
		return PsiUtils.childOfType(this, KScrFileWrapper.class);
	}
	
	public Optional<KScrPackageStatement> getPackage(){
		return wrapper().flatMap(KScrFileWrapper::getPackage);
	}
	
	public String getPackageName(){
		return getPackage().map(KScrPackageStatement::getPackageName).orElse("");
	}
	
	public List<KScrImportStatement> getImports(){
		return wrapper().map(KScrFileWrapper::getImports).orElse(new ArrayList<>(0));
	}
	
	public Optional<KScrType> getTypeDef(){
		return wrapper().flatMap(KScrFileWrapper::getTypeDef);
	}
	
	public @NotNull String getFileName(){
		return getViewProvider().getVirtualFile().getName();
	}
	
	public @NotNull String getName(){
		var o = super.getName();
		if(getTypeDef().isPresent())
			return o.substring(0, o.length() - ".KScr".length());
		return o;
	}
}