package org.comroid.kscr.intellij;

import com.intellij.openapi.fileTypes.LanguageFileType;
import org.jetbrains.annotations.NonNls;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;

public class KScrBinaryFileType extends LanguageFileType implements KScrFileType {

	public static final KScrBinaryFileType FILE_TYPE = new KScrBinaryFileType();

	protected KScrBinaryFileType(){
		super(KScrLanguage.LANGUAGE);
	}
	
	public @NonNls @NotNull String getName(){
		return "KScr File";
	}
	
	public @NotNull String getDescription(){
		return "KScr file";
	}
	
	public @NotNull String getDefaultExtension(){
		return "kscr";
	}
	
	public @Nullable Icon getIcon(){
		return KScrIcons.SINGLE;
	}
}
