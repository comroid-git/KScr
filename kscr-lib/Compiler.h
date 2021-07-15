#pragma once
#include <string>
#include <vector>

#include "TypeCache.h"

class Compiler
{
public:
	explicit Compiler(TypeCache typeCache) : typeCache(typeCache) {}
	void compile(std::vector<std::string> files);
private:
	const TypeCache typeCache;
};

