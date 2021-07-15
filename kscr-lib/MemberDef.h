#pragma once

#include <string>
#include <utility>

class MemberDef
{
public:
	explicit MemberDef() = delete;
	explicit MemberDef(std::string parent, int type, std::string name) : type(type), parent(std::move(parent)), name(std::move(name)) {}
	const int type;
	std::string parent;
	std::string name;
};

