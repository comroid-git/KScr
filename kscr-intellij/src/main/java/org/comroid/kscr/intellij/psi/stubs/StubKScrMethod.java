package org.comroid.kscr.intellij.psi.stubs;

import com.intellij.psi.stubs.EmptyStub;
import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.KScrMethod;
import org.jetbrains.annotations.NotNull;

import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public interface StubKScrMethod extends StubWithKScrModifiers<KScrMethod>{
	
	@NotNull
	String name();
	
	@NotNull
	default List<StubKScrParameter> parameters(){
		return getChildrenStubs().stream()
				.filter(EmptyStub.class::isInstance)
				.flatMap(x -> (Stream<StubElement<?>>)x.getChildrenStubs().stream())
				.filter(StubKScrParameter.class::isInstance)
				.map(StubKScrParameter.class::cast)
				.collect(Collectors.toList());
	}
	
	@NotNull
	String returnTypeText();
	
	boolean hasSemicolon();
}