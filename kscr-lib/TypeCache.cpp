#include "pch.h"
#include "TypeCache.h"

TypeDef* TypeCache::findType(std::string name)
{
	for (iterator it = begin(); it != end(); ++it)
	{
		if (it->first == name)
			return it->second;
	}
	return nullptr;
}

MemberDef* TypeCache::findEntryPoint()
{
	for (iterator it = begin(); it != end(); ++it)
	{
		MemberDef* yield = it->second->findMember("main");
		if (yield != nullptr)
			return yield;
	}
	return nullptr;
}

int TypeCache::runEntryPoint()
{
	MemberDef* entryPoint = findEntryPoint();
	return -2;
}
