#include "pch.h"
#include "TypeDef.h"

MemberDef* TypeDef::findMember(std::string name)
{
	for (iterator it = begin(); it != end(); ++it)
	{
		if (it->first == name)
			return it->second;
	}
	return nullptr;
}

class Parser
{
};

void TypeDef::Parse(std::string name, const std::ifstream& read)
{
	TypeDef out = TypeDef("", name);
	Parser parser = Parser();

	parser
}
