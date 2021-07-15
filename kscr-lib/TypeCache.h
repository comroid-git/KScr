#pragma once
#include <map>
#include <string>

#include "TypeDef.h"

class TypeCache : std::map<std::string, TypeDef*>
{
public:
	explicit TypeCache() = default;
	TypeDef* findType(std::string name);
	MemberDef* findEntryPoint();
	void runEntryPoint();
};

