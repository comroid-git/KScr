#pragma once
#include <string>
#include <vector>

#include "TypeCache.h"

class Compiler
{
public:
	explicit Compiler(TypeCache typeCache) : typeCache(typeCache) {}
	static void compileTypes(std::vector<std::string>* files);
	static void compileBodies();
private:
	const TypeCache typeCache;
};

