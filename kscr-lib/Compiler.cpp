#include "pch.h"
#include "Filesystem.h"
#include "Compiler.h"

#include <fstream>

#include "Const.h"

void Compiler::compileTypes(std::vector<std::string> files)
{
	for (std::string file : files)
	{
		std::string name = Filesystem::simpleFileName(file);
		std::ifstream read = std::ifstream(file);
		TypeDef out = TypeDef("", name);
		char* buf;
		std::streampos size;

		if (read.is_open())
		{
			size = read.tellg();
			buf = new char[size];
			read.seekg(0, std::ios::beg);
			read.read(buf, size);
			parser.digest(buf);
		}
		read.close();

		TypeDef typeDef = parser.finalize();
		Const::typeCache[typeDef.name] = &typeDef;
	}
}

void Compiler::compileBodies()
{
}
