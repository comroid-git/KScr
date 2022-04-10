using KScr.Core.Model;

namespace KScr.Bytecode.Model;

public class WritableClass : IBytecode
{
    public readonly Core.Std.Class MainClass;
    public readonly Core.Std.Class[] SubClasses;

    public WritableClass(Core.Std.Class mainClass, params Core.Std.Class[] subClasses)
    {
        MainClass = mainClass;
        SubClasses = subClasses;
    }

    public BytecodeElementType ElementType => BytecodeElementType.ClassFile;
}