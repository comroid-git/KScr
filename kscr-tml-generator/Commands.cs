using CommandLine;

namespace KScr.TmlGenerator;

public interface IBaseCmd
{
    [Option('o', "output", Default = "kscr.tmLanguage")]
    public FileInfo OutputFile { get; set; }
}

[Verb("gen", isDefault: true)]
public class GenerateCommand : IBaseCmd
{
    public FileInfo OutputFile { get; set; }
}
