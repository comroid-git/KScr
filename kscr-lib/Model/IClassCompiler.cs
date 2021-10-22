using System.Collections.Generic;
using System.IO;
using KScr.Lib.Bytecode;
using KScr.Lib.Store;

namespace KScr.Lib.Model
{
    public enum ClassCompilerState
    {
        Idle,
        Package,
        Class
    }
    
    public interface IClassCompiler
    {
        public ClassCompilerState State { get; }
        public Package CompilePackage(RuntimeBase vm, DirectoryInfo dir);
        public void CompileClasses(RuntimeBase vm, DirectoryInfo dir);
        public Class CompileClass(RuntimeBase vm, FileInfo file);
        public IClassCompiler NextPackage(string name);
        public IClassCompiler NextClass(string name);
        public IClassCompiler PushElement();
        public IClassCompiler AcceptToken(RuntimeBase vm, IList<ClassToken> tokens, ref int i);
    }
}