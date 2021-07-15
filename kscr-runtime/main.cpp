#include <iostream>
#include <string>
#include <vector>
#include "../kscr-lib/Filesystem.h"
#include "../kscr-lib/Compiler.h"

struct Const
{
	static std::vector<std::string> files;
	static TypeCache typeCache;
};

void compile()
{
	Compiler compiler(Const::typeCache);
	compiler.compile(Const::files);
}

void run()
{
	Const::typeCache.runEntryPoint();
}

auto main(int argc, char* argv[]) -> int
{
	std::cout << "hello world; i am kscr";
	for (int i = 0; i < argc; ++i)
	{
		std::string str(argv[i]);

		// take input files
		if (str.find(".kscr") && Filesystem::exists(str))
			Const::files.push_back(str);
	}
	
	compile();
	run();
}
