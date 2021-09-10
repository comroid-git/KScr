#include "pch.h"
#include "Numeric.h"

#include <map>

std::regex Numeric::NumberRegex = std::regex("([\d]+)(i|l|f|d)?(\.([\d]+)(f|d)?)?");
std::map<const void*, Numeric*> constants = std::map<const void*, Numeric*>();

Numeric Numeric::constant(int intValue)
{
	const void* kptr = &intValue;
	Numeric value = Numeric(true, MODE_INT);
	value.IntValue = intValue;
	Numeric* vptr = &value;
	constants[kptr] = vptr;
	return value;
}

Numeric Numeric::constant(long longValue)
{
	const void* kptr = &longValue;
	Numeric value = Numeric(true, MODE_LONG);
	value.LongValue = longValue;
	Numeric* vptr = &value;
	constants[kptr] = vptr;
	return value;
}

Numeric Numeric::constant(float floatValue)
{
	const void* kptr = &floatValue;
	Numeric value = Numeric(true, MODE_FLOAT);
	value.FloatValue = floatValue;
	Numeric* vptr = &value;
	constants[kptr] = vptr;
	return value;
}

Numeric Numeric::constant(double doubleValue)
{
	const void* kptr = &doubleValue;
	Numeric value = Numeric(true, MODE_DOUBLE);
	value.DoubleValue = doubleValue;
	Numeric* vptr = &value;
	constants[kptr] = vptr;
	return value;
}

Numeric Numeric::constant(char byteValue)
{
	const void* kptr = &byteValue;
	Numeric value = Numeric(true, MODE_BYTE);
	value.ByteValue = byteValue;
	Numeric* vptr = &value;
	constants[kptr] = vptr;
	return value;
}
