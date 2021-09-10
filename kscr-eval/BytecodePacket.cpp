#include "pch.h"
#include "BytecodePacket.h"

auto BytecodePacket::evaluate(std::map<const char*, void*>* obj_map)
{
	void* result;
	void* altResult;

	// evaluate alternate packet first
	if (altPacket != nullptr)
		altResult = altPacket->evaluate(obj_map);

	if ((type & BytecodePacket::DECLARATION) == 0)
	{
		obj_map->insert(std::make_pair(arg, altResult));
	} else if ((type & BytecodePacket::EXPRESSION_NUMERIC) == 0)
	{
	}
	else if ((type & BytecodePacket::EXPRESSION_STRING) == 0) {}

	return result;
}
