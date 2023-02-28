using CommandLine;
using KScr.Antlr;

namespace KScr.TmlGenerator;

public static class KScrTmlGenerator
{
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<GenerateCommand>(args)
            .WithParsed(RunGenerate);
    }

    private static void RunGenerate(GenerateCommand obj)
    {
    }
}
