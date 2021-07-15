#pragma once
#include <map>
#include <string>
#include <utility>
#include "MemberDef.h"

class TypeDef : std::map<std::string, MemberDef*>
{
public:
	explicit TypeDef() = default;
	explicit TypeDef(std::string parent, std::string name) : parent(std::move(parent)), name(std::move(name)) {}
	const std::string parent;
	const std::string name;
	MemberDef* findMember(std::string name);
	static void Parse(std::string file);
};

