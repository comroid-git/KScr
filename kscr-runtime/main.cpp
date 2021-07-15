#include <iostream>
#include <string>
#include <vector>
#include "../kscr-lib/Filesystem.h"
#include "../kscr-lib/Compiler.h"
#include "../kscr-lib/TypeCache.h"

std::vector<std::string> files = std::vector<std::string>();
TypeCache typeCache = TypeCache();

void compile()
{
	Compiler compiler(typeCache);
	compiler.compile(files);
}

void run()
{
	typeCache.runEntryPoint();
}

auto main(int argc, char* argv[]) -> int
{
	std::cout << "hello world; i am kscr";
	for (int i = 0; i < argc; ++i)
	{
		std::string str(argv[i]);

		// take input files
		if (str.find(".kscr") && Filesystem::exists(str))
			files.push_back(str);
	}
	
	compile();
	run();
}
