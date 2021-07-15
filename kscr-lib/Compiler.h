#pragma once
#include <string>
#include <vector>

class Compiler
{
public:
	static void compileTypes(std::vector<std::string>* files);
	static void compileBodies();
private:
	explicit Compiler() {}
};

