package org.comroid.kscr.intellij.psi.utils;

import com.intellij.lang.jvm.JvmClass;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.roots.ProjectRootManager;
import com.intellij.openapi.vfs.VirtualFile;
import com.intellij.psi.JavaPsiFacade;
import com.intellij.psi.PsiManager;
import com.intellij.psi.search.GlobalSearchScope;
import org.comroid.kscr.intellij.KScrFileType;
import org.comroid.kscr.intellij.KScrSourceFileType;
import org.comroid.kscr.intellij.psi.*;
import org.comroid.kscr.intellij.psi.types.JvmKScrClass;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.*;
import java.util.concurrent.atomic.AtomicReference;
import java.util.function.Predicate;

public class ProjectTypeFinder{
	
	@Nullable
	public static JvmClass firstType(Project p, List<String> candidates){
		for(String candidate : candidates){
			var type = ProjectTypeFinder.findByName(p, candidate);
			if(type.isPresent())
				return type.get();
		}
		return null;
	}
	
	public static List<JvmClass> allVisibleAt(Project in, KScrElement at){
		// TODO: check visibility modifiers
		var types = findAll(in, x -> true);
		addTypesFromPackage(in, types, "java.lang");
		for(KScrImportStatement imp : at.getContainer().map(KScrFileWrapper::getImports).orElse(Collections.emptyList())){
			if(imp.isWildcard())
				addTypesFromPackage(in, types, imp.getImportName());
			else if(!imp.isWildcard()){
				var t = JavaPsiFacade.getInstance(in).findClass(imp.getImportName(), GlobalSearchScope.allScope(in));
				if(t != null)
					types.add(t);
			}
		}
		return types;
	}
	
	private static void addTypesFromPackage(Project in, List<JvmClass> types, String pkg){
		var aPackage = JavaPsiFacade.getInstance(in).findPackage(pkg);
		if(aPackage != null)
			types.addAll(Arrays.asList(aPackage.getClasses()));
	}
	
	public static Optional<JvmClass> findByName(Project in, String qualifiedName){
		// first check types in project
		// can appear in any file due to inner types
		return find(in, y -> y.fullyQualifiedName().equals(qualifiedName), null)
				.or(() -> Optional.ofNullable(JavaPsiFacade.getInstance(in).findClass(qualifiedName, GlobalSearchScope.allScope(in))));
	}
	
	/**
	 * Checks every KScr type in a project and returns the first type found that matches the criteria, or an empty
	 * Optional if none exist.
	 *
	 * @param in
	 * 		The project to search in.
	 * @param checker
	 * 		The criteria to search against.
	 * @return The first type found that matches the criteria.
	 */
	public static Optional<JvmClass> find(@NotNull Project in, @NotNull Predicate<KScrType> checker, @Nullable Predicate<VirtualFile> directCheck){
		AtomicReference<Optional<JvmClass>> ret = new AtomicReference<>(Optional.empty());
		ProjectRootManager.getInstance(in).getFileIndex().iterateContent(vf -> {
			if(directCheck != null && !directCheck.test(vf))
				return true;
			if(!vf.isDirectory() && Objects.equals(vf.getExtension(), "KScr") && vf.getFileType() == KScrSourceFileType.FILE_TYPE){
				KScrFile file = (KScrFile)PsiManager.getInstance(in).findFile(vf);
				if(file != null){
					var type = file.getTypeDef();
					if(type.isPresent()){
						var checked = checkTypeAndMembers(type.get(), checker);
						if(checked.isPresent()){
							ret.set(checked);
							return false;
						}
					}
				}
			}
			return true;
		});
		return ret.get();
	}
	
	public static List<JvmClass> findAll(@NotNull Project in, @NotNull Predicate<KScrType> checker){
		List<JvmClass> ret = new ArrayList<>();
		ProjectRootManager.getInstance(in).getFileIndex().iterateContent(vf -> {
			if(!vf.isDirectory() && Objects.equals(vf.getExtension(), "KScr") && vf.getFileType() == KScrSourceFileType.FILE_TYPE){
				KScrFile file = (KScrFile)PsiManager.getInstance(in).findFile(vf);
				if(file != null){
					var type = file.getTypeDef();
					if(type.isPresent()){
						var checked = checkTypeAndMembers(type.get(), checker);
						if(checked.isPresent()){
							ret.add(checked.get());
							return true; // don't stop
						}
					}
				}
			}
			return true;
		});
		return ret;
	}
	
	public static Optional<JvmClass> checkTypeAndMembers(@NotNull KScrType type, @NotNull Predicate<KScrType> checker){
		if(checker.test(type))
			return Optional.of(JvmKScrClass.of(type));
		for(KScrMember member : type.getMembers()){
			Optional<KScrType> def = PsiUtils.childOfType(member, KScrType.class);
			if(def.isPresent()){
				var r = checkTypeAndMembers(def.get(), checker);
				if(r.isPresent())
					return r;
			}
		}
		return Optional.empty();
	}
}