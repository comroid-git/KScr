#include "pch.h"
#include "Numeric.h"

#include <map>

std::regex Numeric::NumberRegex = std::regex("([\\d]+)(i|l|f|d)?(\\.([\\d]+)(f|d)?)?");
static std::map<const void*, Numeric*> constants = std::map<const void*, Numeric*>();

Numeric* Numeric::constant(int intValue)
{
	const void* kptr = &intValue;
	Numeric value = Numeric(true, MODE_INT);
	value.IntValue = intValue;
	Numeric* vptr = &value;
	constants[kptr] = vptr;
	return vptr;
}

Numeric* Numeric::constant(long longValue)
{
	const void* kptr = &longValue;
	Numeric value = Numeric(true, MODE_LONG);
	value.LongValue = longValue;
	Numeric* vptr = &value;
	constants[kptr] = vptr;
	return vptr;
}

Numeric* Numeric::constant(float floatValue)
{
	const void* kptr = &floatValue;
	Numeric value = Numeric(true, MODE_FLOAT);
	value.FloatValue = floatValue;
	Numeric* vptr = &value;
	constants[kptr] = vptr;
	return vptr;
}

Numeric* Numeric::constant(double doubleValue)
{
	const void* kptr = &doubleValue;
	Numeric value = Numeric(true, MODE_DOUBLE);
	value.DoubleValue = doubleValue;
	Numeric* vptr = &value;
	constants[kptr] = vptr;
	return vptr;
}

Numeric* Numeric::constant(char byteValue)
{
	const void* kptr = &byteValue;
	Numeric value = Numeric(true, MODE_BYTE);
	value.ByteValue = byteValue;
	Numeric* vptr = &value;
	constants[kptr] = vptr;
	return vptr;
}

Numeric* Numeric::parse(char* str)
{
	std::smatch matches;
	std::string string = std::string(str);
	if (!std::regex_search(string, matches, NumberRegex))
		throw std::invalid_argument("Not a valid number!");
	const std::string type = matches[matches.size() == 5 ? 1 : 0].str();
	Numeric* result;
	if (type == "b")
		result = constant(*str);
	else if (type == "i")
		result = constant(std::stoi(string));
	else if (type == "l")
		result = constant(std::stol(string));
	else if (type == "f")
		result = constant(std::stof(string));
	else if (type == "d")
		result = constant(std::stod(string));
	else if (matches[2].length() > matches[3].length())
		result = constant(std::stof(string));
	else result = constant(std::stoi(string));
	return result;
}

Numeric* Numeric::plus(Numeric* right)
{
	if (mode == right->mode)
	{
		if (mode == MODE_BYTE)
			return constant(ByteValue + right->ByteValue);
		if (mode == MODE_INT)
			return constant(IntValue + right->IntValue);
		if (mode == MODE_LONG)
			return constant(LongValue + right->LongValue);
		if (mode == MODE_FLOAT)
			return constant(FloatValue + right->FloatValue);
		if (mode == MODE_DOUBLE)
			return constant(DoubleValue + right->DoubleValue);
	}
}

Numeric* Numeric::minus(Numeric* right)
{
	if (mode == right->mode)
	{
		if (mode == MODE_BYTE)
			return constant(ByteValue - right->ByteValue);
		if (mode == MODE_INT)
			return constant(IntValue - right->IntValue);
		if (mode == MODE_LONG)
			return constant(LongValue - right->LongValue);
		if (mode == MODE_FLOAT)
			return constant(FloatValue - right->FloatValue);
		if (mode == MODE_DOUBLE)
			return constant(DoubleValue - right->DoubleValue);
	}
}

Numeric* Numeric::multiply(Numeric* right)
{
	if (mode == right->mode)
	{
		if (mode == MODE_BYTE)
			return constant(ByteValue * right->ByteValue);
		if (mode == MODE_INT)
			return constant(IntValue * right->IntValue);
		if (mode == MODE_LONG)
			return constant(LongValue * right->LongValue);
		if (mode == MODE_FLOAT)
			return constant(FloatValue * right->FloatValue);
		if (mode == MODE_DOUBLE)
			return constant(DoubleValue * right->DoubleValue);
	}
}

Numeric* Numeric::divide(Numeric* right)
{
	if (mode == right->mode)
	{
		if (mode == MODE_BYTE)
			return constant(ByteValue / right->ByteValue);
		if (mode == MODE_INT)
			return constant(IntValue / right->IntValue);
		if (mode == MODE_LONG)
			return constant(LongValue / right->LongValue);
		if (mode == MODE_FLOAT)
			return constant(FloatValue / right->FloatValue);
		if (mode == MODE_DOUBLE)
			return constant(DoubleValue / right->DoubleValue);
	}
}

Numeric* Numeric::modulus(Numeric* right)
{
	if (mode == right->mode)
	{
		if (mode == MODE_BYTE)
			return constant(ByteValue % right->ByteValue);
		if (mode == MODE_INT)
			return constant(IntValue % right->IntValue);
		if (mode == MODE_LONG)
			return constant(LongValue % right->LongValue);
	}
}
