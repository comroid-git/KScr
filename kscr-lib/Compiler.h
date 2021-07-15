#pragma once
#include <map>
#include <string>
#include <vector>

#include "TypeCache.h"
#include "TypeDef.h"

class Compiler
{
public:
	explicit Compiler(TypeCache typeCache) : typeCache(typeCache) {}
	void compile(std::vector<std::string> files);
private:
	const TypeCache typeCache;
};

