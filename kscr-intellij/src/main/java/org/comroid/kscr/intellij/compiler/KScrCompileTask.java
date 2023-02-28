package org.comroid.kscr.intellij.compiler;

import com.intellij.openapi.compiler.CompileContext;
import com.intellij.openapi.compiler.CompileTask;

import java.nio.file.Paths;

public class KScrCompileTask implements CompileTask {
    @Override
    public boolean execute(CompileContext context) {
        var project = context.getProject();
        if (project.getBasePath() == null)
            return false;
        var modFile = Paths.get(project.getBasePath(), "module.kmod.json").toFile();
        var srcDir = Paths.get(project.getBasePath(), "src").toFile();
        var buildDir = Paths.get(project.getBasePath(), "build").toFile();

        if (modFile.exists())
        {
            // compile using kbuild executable
            return false;
        }

        // compile using kscr executable
        return false;
    }
}
