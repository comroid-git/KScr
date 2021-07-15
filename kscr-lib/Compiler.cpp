#include "pch.h"
#include "Filesystem.h"
#include "Compiler.h"

#include <fstream>

void Compiler::compileTypes(std::vector<std::string> files)
{
	for (std::string file : files)
	{
		TypeDef::Parse(file);
	}
}

void Compiler::compileBodies()
{
}
