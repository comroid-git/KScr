#include "pch.h"
#include "BytecodePacket.h"

#include "../kscr-lib/Numeric.h"
#include "../kscr-lib/String.h"

void* operatorPlus(void* left, void* right)
{
	if (typeid(left) == typeid(Numeric) && typeid(right) == typeid(Numeric))
		return static_cast<Numeric*>(left)->plus(static_cast<Numeric*>(right));
}
void* operatorMinus(void* left, void* right)
{
	if (typeid(left) == typeid(Numeric) && typeid(right) == typeid(Numeric))
		return static_cast<Numeric*>(left)->minus(static_cast<Numeric*>(right));
}
void* operatorMultiply(void* left, void* right)
{
	if (typeid(left) == typeid(Numeric) && typeid(right) == typeid(Numeric))
		return static_cast<Numeric*>(left)->multiply(static_cast<Numeric*>(right));
}
void* operatorDivide(void* left, void* right)
{
	if (typeid(left) == typeid(Numeric) && typeid(right) == typeid(Numeric))
		return static_cast<Numeric*>(left)->divide(static_cast<Numeric*>(right));
}
void* operatorModulus(void* left, void* right)
{
	if (typeid(left) == typeid(Numeric) && typeid(right) == typeid(Numeric))
		return static_cast<Numeric*>(left)->modulus(static_cast<Numeric*>(right));
}

void* BytecodePacket::evaluate(const Bytecode* bytecode, const BytecodePacket* prev, void* prevResult, std::map<const char*, void*>* obj_map)
{
	void* result = nullptr;
	void* altResult = nullptr;
	void* subResult = nullptr;

	// evaluate alternate packet 1 first
	if (altPacketEindex != -1)
	{
		BytecodePacket altPacket = bytecode->extra.at(altPacketEindex);
		altResult = altPacket.evaluate(bytecode, this, result, obj_map);
	}
	if (subPacketEindex != -1)
	{
		BytecodePacket subPacket = bytecode->extra.at(subPacketEindex);
		subResult = subPacket.evaluate(bytecode, this, result, obj_map);
	}

	if ((type & DECLARATION) != 0)
		obj_map->insert(std::make_pair(static_cast<char*>(arg), altResult));
	else if ((type & ASSIGNMENT) != 0 && (prev->type & DECLARATION) != 0)
		obj_map->insert(std::make_pair(static_cast<char*>(prev->arg), subResult));
	else if (type == LITERAL_NUMERIC)
		result = arg;
	else if (type == LITERAL_STRING)
		result = String::instance(static_cast<char*>(arg));
	else if (type == LITERAL_TRUE)
		result = Numeric::constant(static_cast<char>(1));
	else if (type == LITERAL_FALSE)
		result = Numeric::constant(static_cast<char>(-1));
	else if ((type & EXPRESSION_VAR) != 0)
	{
		char* key = static_cast<char*>(arg);
		if (obj_map->count(key) == 1)
			result = obj_map->at(key);
		else obj_map->insert(std::pair<char*, void*>(key, nullptr));
	}
	else if ((type & OPERATOR) != 0)
	{
		if ((type & OPERATOR_PLUS) != 0)
			return operatorPlus(prevResult, altResult);
		if ((type & OPERATOR_MINUS) != 0)
			return operatorMinus(prevResult, altResult);
		if ((type & OPERATOR_MULTIPLY) != 0)
			return operatorMultiply(prevResult, altResult);
		if ((type & OPERATOR_DIVIDE) != 0)
			return operatorDivide(prevResult, altResult);
		if ((type & OPERATOR_MODULUS) != 0)
			return operatorModulus(prevResult, altResult);
	}

	return result;
}
