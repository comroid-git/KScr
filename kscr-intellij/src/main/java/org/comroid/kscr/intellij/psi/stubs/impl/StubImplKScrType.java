package org.comroid.kscr.intellij.psi.stubs.impl;

import com.intellij.psi.stubs.StubBase;
import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.types.KScrType;
import org.comroid.kscr.intellij.psi.stubs.StubKScrType;
import org.comroid.kscr.intellij.psi.stubs.StubTypes;
import org.comroid.kscr.intellij.psi.types.KScrKind;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

public class StubImplKScrType extends StubBase<KScrType> implements StubKScrType{
	
	@NotNull
	private final String fqName;
	@NotNull
	private final String shortName;
	@NotNull
	private final KScrKind kind;
	
	public StubImplKScrType(@Nullable StubElement parent, KScrType type){
		this(parent, type.getName(), type.getName(), type.kind());
	}
	
	public StubImplKScrType(@Nullable StubElement parent, String shortName, String fqName, KScrKind kind){
		super(parent, StubTypes.KScr_TYPE);
		this.shortName = shortName;
		this.fqName = fqName;
		this.kind = kind;
	}
	
	@NotNull
	public String fullyQualifiedName(){
		return fqName;
	}
	
	@NotNull
	public String shortName(){
		return shortName;
	}
	
	@NotNull
	public KScrKind kind(){
		return kind;
	}
}