#pragma once
#include <string>
#include <vector>

#include "TypeCache.h"

class Compiler
{
public:
	explicit Compiler(TypeCache typeCache) : typeCache(typeCache) {}
	void compileTypes(std::vector<std::string> files);
	void compileBodies();
private:
	const TypeCache typeCache;
};

