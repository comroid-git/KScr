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
	return nullptr;
}
void* operatorMultiply(void* left, void* right)
{
	return nullptr;
}
void* operatorDivide(void* left, void* right)
{
	return nullptr;
}
void* operatorModulus(void* left, void* right)
{
	return nullptr;
}

void* BytecodePacket::evaluate(BytecodePacket* prev, void* prevResult, std::map<const char*, void*>* obj_map)
{
	void* result = nullptr;
	void* altResult;

	// evaluate alternate packet first
	if (altPacket != nullptr)
		altResult = altPacket->evaluate(this, result, obj_map);

	if ((type & DECLARATION) == 0)
	{
		obj_map->insert(std::make_pair(static_cast<char*>(arg), altResult));
	}
	else if ((type & EXPRESSION_NUMERIC) == 0)
		result = Numeric::parse(static_cast<char*>(arg));
	else if ((type & EXPRESSION_STRING) == 0)
		result = String::instance(static_cast<char*>(arg));
	else if ((type & EXPRESSION_TRUE) == 0)
		result = Numeric::constant(static_cast<char>(1));
	else if ((type & EXPRESSION_FALSE) == 0)
		result = Numeric::constant(static_cast<char>(-1));
	else if ((type & EXPRESSION_VAR) == 0)
		result = obj_map->at(static_cast<char*>(arg));
	else if ((type & OPERATOR) == 0)
	{
		void* rightResult = followupPacket->evaluate(this, result, obj_map);
		if ((type & OPERATOR_PLUS) == 0)
			return operatorPlus(prevResult, rightResult);
		if ((type & OPERATOR_MINUS) == 0)
			return operatorMinus(prevResult, rightResult);
		if ((type & OPERATOR_MULTIPLY) == 0)
			return operatorMultiply(prevResult, rightResult);
		if ((type & OPERATOR_DIVIDE) == 0)
			return operatorDivide(prevResult, rightResult);
		if ((type & OPERATOR_MODULUS) == 0)
			return operatorModulus(prevResult, rightResult);
	}

	return result;
}
