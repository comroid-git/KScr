#include <iostream>
#include <string>
#include <vector>
#include "../kscr-lib/Const.h"
#include "../kscr-lib/Filesystem.h"
#include "../kscr-lib/Compiler.h"

void compile()
{
	Compiler::compileTypes(&Const::files);
	Compiler::compileBodies();
}

int run()
{
	return Const::typeCache.runEntryPoint();
}

auto main(int argc, char* argv[]) -> int
{
	if (argc == 0)
	{
		std::cout << "Missing source";
		return -1;
	}
	for (int i = 0; i < argc; ++i)
	{
		std::string str(argv[i]);

		// take input files
		if (str.find(".kscr") && Filesystem::exists(str))
			Const::files.push_back(str);
	}
	
	compile();
	const int exitCode = run();
	std::cout << "Program finished with exit code " << exitCode << std::endl;
	return exitCode;
}
