package org.comroid.kscr.intellij.psi.stubs;

import com.intellij.psi.stubs.StubElement;
import org.comroid.kscr.intellij.psi.ast.types.KScrRecordComponents;
import org.jetbrains.annotations.NotNull;

import java.util.List;
import java.util.stream.Collectors;

public interface StubKScrRecordComponents extends StubElement<KScrRecordComponents>{
	
	@NotNull
	default List<StubKScrParameter> components(){
		return getChildrenStubs().stream()
				.filter(StubKScrParameter.class::isInstance)
				.map(StubKScrParameter.class::cast)
				.collect(Collectors.toList());
	}
}