#include <iostream>
#include <string>
#include <vector>
#include "../kscr-lib/Const.h"
#include "../kscr-lib/Filesystem.h"
#include "../kscr-lib/Tokenizer.h"
#include "../kscr-eval/Eval.h"

void compile()
{
	Tokenizer::tokenize(&Const::files);
}

int run()
{
	return Const::typeCache.runEntryPoint();
}

// eval test
auto main(int argc, char* argv[]) -> int
{
	// debug code statement
	const char* code = "num a = 1; num b = 2.2; return a + b;";

	// compile into bytecode
	const char* lib = Eval::compile(code);

	// run bytecode
	return Eval::execute(lib);
}

/* // lib test
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
*/
