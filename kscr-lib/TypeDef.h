#pragma once
#include <map>
#include <string>
#include <utility>
#include "MemberDef.h"

class TypeDef
{
public:
	explicit TypeDef(std::string parent, std::string name) : parent(std::move(parent)), name(std::move(name)) {}
	const std::map<std::string, MemberDef> members = std::map<std::string, MemberDef>();
	const std::string parent;
	const std::string name;
	MemberDef findMember(std::string name);
};

