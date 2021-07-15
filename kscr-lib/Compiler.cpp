#include "pch.h"
#include "Filesystem.h"
#include "Compiler.h"

#include <fstream>

void Compiler::compile(std::vector<std::string> files)
{
	for (std::string file : files)
	{
		std::ifstream fread;
		fread.open(file);
		TypeDef::Parse(Filesystem::simpleFileName(file), fread);
	}
}
