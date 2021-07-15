#pragma once

#include "TypeCache.h"
#include "TypeDef.h"

#define FIELD 1;
#define PROPERTY 2;
#define METHOD 3;

class MemberDef
{
public:
	explicit MemberDef(const TypeDef parent, const int type, std::string name) : type(type), parent(parent), name(name), typeCache(typeCache)
	{
	}
	const int type;
	const TypeDef parent;
	const std::string name;
	void runEntryPoint();
private:
	const TypeCache typeCache;
};

