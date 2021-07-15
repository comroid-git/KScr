#include "pch.h"
#include "Filesystem.h"
#include "Compiler.h"

#include <fstream>

#include "Const.h"

void digest(char buf[])
{
}

void Compiler::compileTypes(std::vector<std::string>* files)
{
	for (std::string file : files->begin())
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
			digest(buf);
		}
		read.close();
	}
}

void Compiler::compileBodies()
{
}
