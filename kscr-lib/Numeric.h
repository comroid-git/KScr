#pragma once
#include <regex>

class Numeric
{
	explicit Numeric(bool readonly, char mode) : readonly(readonly), mode(mode) {}
public:
	static std::regex NumberRegex;
	// operating mode constants
	static constexpr char MODE_INT      = 0x1;
	static constexpr char MODE_LONG     = 0x2;
	static constexpr char MODE_FLOAT    = 0x4;
	static constexpr char MODE_DOUBLE   = 0x8;
	static constexpr char MODE_BYTE     = 0xF;

	explicit Numeric() : readonly(true), mode(MODE_BYTE)
	{
	}
	static Numeric constant(int intValue);
	static Numeric constant(long longValue);
	static Numeric constant(float floatValue);
	static Numeric constant(double doubleValue);
	static Numeric constant(char byteValue);
	static Numeric* parse(char* str);

	// class logic
	bool readonly;
	char mode;

	int IntValue = 0;
	long LongValue = 0;
	float FloatValue = 0;
	double DoubleValue = 0;
	char ByteValue = 0;
};
