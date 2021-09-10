#include "pch.h"
#include "Numeric.h"

#include <map>

std::regex Numeric::NumberRegex = std::regex("([\d]+)(i|l|f|d)?(\.([\d]+)(f|d)?)?");
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
	if (!std::regex_search(std::string(str), matches, NumberRegex))
		throw std::invalid_argument("Not a valid number!");
	const std::string type = matches[matches.size() == 5 ? 1 : 0].str();
	Numeric* result;
	if (type == "b")
		result = constant(*str);
	else if (type == "i")
		result = constant(std::stoi(str));
	else if (type == "l")
		result = constant(std::stol(str));
	else if (type == "f")
		result = constant(std::stof(str));
	else if (type == "d")
		result = constant(std::stod(str));
	else if (matches[2].length() > matches[3].length())
		result = constant(std::stof(str));
	else result = constant(std::stoi(str));
	return result;
}
