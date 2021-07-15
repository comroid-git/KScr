#pragma once
#include <map>
#include <string>
#include <utility>
#include <vector>
#include "MemberDef.h"
#include "TypeCache.h"

class TypeDef
{
public:
	explicit TypeDef(std::string parent, std::string name) : typeCache(typeCache), parent(std::move(parent)), name(std::move(name)) {}
	const std::map<std::string, MemberDef> members = std::map<std::string, MemberDef>();
	const std::string parent;
	const std::string name;
	MemberDef findMember(std::string name);
private:
	const TypeCache typeCache;
};

