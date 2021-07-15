#pragma once

#include <string>
#include <utility>

#define FIELD 1;
#define PROPERTY 2;
#define METHOD 3;

class MemberDef
{
public:
	explicit MemberDef() = delete;
	explicit MemberDef(std::string parent, int type, std::string name) : type(type), parent(std::move(parent)), name(std::move(name)) {}
	const int type;
	std::string parent;
	std::string name;
};

