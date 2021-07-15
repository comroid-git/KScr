#include "pch.h"
#include "TypeDef.h"

#include <fstream>
#include <iostream>

#include "Filesystem.h"
#include "Const.h"

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
