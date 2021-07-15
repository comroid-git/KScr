#pragma once

#include <string>
#include <utility>

class MemberDef
{
public:
	explicit MemberDef() = default;
	explicit MemberDef(std::string parent, int type, std::string name, int modifier) : type(type), modifier(modifier), parent(std::move(parent)), name(std::move(name)) {}
	const int type;
	const int modifier;
	std::string parent;
	std::string name;
};

