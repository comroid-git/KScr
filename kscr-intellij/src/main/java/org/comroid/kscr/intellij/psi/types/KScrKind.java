package org.comroid.kscr.intellij.psi.types;

import com.intellij.lang.jvm.JvmClassKind;

public enum KScrKind{
	CLASS, INTERFACE, ENUM, RECORD, ANNOTATION, SINGLE, CONSTRUCTED;
	
	@SuppressWarnings("UnstableApiUsage")
	public JvmClassKind toJvmKind(){
		switch(this){
			default:
			case CLASS:
				return JvmClassKind.CLASS;
			case INTERFACE:
				return JvmClassKind.INTERFACE;
			case ENUM:
				return JvmClassKind.ENUM;
			case ANNOTATION:
				return JvmClassKind.ANNOTATION;
		}
	}
}
