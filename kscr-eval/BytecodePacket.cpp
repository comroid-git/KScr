#include "pch.h"
#include "BytecodePacket.h"
#include "../kscr-lib/Numeric.h"
#include "../kscr-lib/String.h"

void* BytecodePacket::evaluate(std::map<const char*, void*>* obj_map)
{
	void* result = nullptr;
	void* altResult;

	// evaluate alternate packet first
	if (altPacket != nullptr)
		altResult = altPacket->evaluate(obj_map);

	if ((type & BytecodePacket::DECLARATION) == 0)
	{
		obj_map->insert(std::make_pair(static_cast<char*>(arg), altResult));
	}
	else if ((type & BytecodePacket::EXPRESSION_NUMERIC) == 0)
		return Numeric::parse(static_cast<char*>(arg));
	else if ((type & BytecodePacket::EXPRESSION_STRING) == 0)
		return String::instance(static_cast<char*>(arg));
	else if ((type & BytecodePacket::EXPRESSION_TRUE) == 0)
		return Numeric::constant(static_cast<char>(1));
	else if ((type & BytecodePacket::EXPRESSION_FALSE) == 0)
		return Numeric::constant(static_cast<char>(-1));
	else if ((type & BytecodePacket::EXPRESSION_VAR) == 0)
		return obj_map->at(static_cast<char*>(arg));

	return result;
}
