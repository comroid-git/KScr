#include "pch.h"
#include "TypeDef.h"

#include <fstream>
#include <iostream>

#include "Filesystem.h"

using namespace std;

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
private:
	std::string source = "";
	int cnt = 0;
public:
	void digest(char c)
	{
		source += c;
		cnt++;
	}

	TypeDef finalize();
};

void TypeDef::Parse(std::string file)
{
	std::string name = Filesystem::simpleFileName(file);
	std::ifstream read = std::ifstream(file, ios::in | ios::binary | ios::ate);
	TypeDef out = TypeDef("", name);
	Parser parser = Parser();
	char* buf = new char[1];
	streampos size;
	
	if (read.is_open())
	{
		size = read.tellg();
		read.seekg(0, ios::beg);
		read.read(buf, size);
		parser.digest(buf[0]);
	}

	read.close();
	TypeDef typeDef = parser.finalize();
}
