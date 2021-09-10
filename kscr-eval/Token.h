#pragma once
#include <iostream>

class Token
{
public:
	// token class definitions
	static constexpr char PACKET_TERMINATOR = 0xFFFF;

	// basic symbols
	static constexpr int TERMINATOR  = 0x0000; // ';' statement termination
	static constexpr int VAR         = 0x0001; // variable identifier
	static constexpr int VAR_ident   = 0x0002; // 'var'-declaration
	static constexpr int NUM_ident   = 0x0004; // 'num'-declaration
	static constexpr int BYTE_ident  = 0x0008; // 'byte'-declaration
	static constexpr int STR_ident   = 0x000F; // 'str'-declaration
	static constexpr int VOID_ident  = 0x0010;
	static constexpr int NUM_LITERAL = 0x0020;
	static constexpr int STR_LITERAL = 0x0040;
	static constexpr int reserved4   = 0x0080;
	static constexpr int RETURN      = 0x00F0; // 'return'-statement

	// arithmetic operators
	static constexpr int PLUS        = 0x0100; // + symbol
	static constexpr int MINUS       = 0x0200; // - symbol
	static constexpr int MULTIPLY    = 0x0400; // * symbol
	static constexpr int DIVIDE      = 0x0800; // / symbol
	static constexpr int MODULUS     = 0x0F00; // % symbol

	explicit Token() : type(0), arg(nullptr), complete(false) {}
	explicit Token(int type) : type(type), arg(nullptr), complete(true) {}
	explicit Token(int type, char* arg) : type(type), arg(arg), complete(true) {}

	static Token fromBytes(char* bytes)
	{
		// todo
	}

	char* toBytes()
	{
		int arglen = sizeof arg;
		int bytelen = 4 + arglen + 1;
		char* bytes = static_cast<char*>(malloc(bytelen));

		// copy type
		char* typebytes = static_cast<char*>(static_cast<void*>(&type));

		// copy all
		for (int i = 0; i < bytelen; i++)
		{
			char* ptr = bytes + i;

			// copy bytes
			if (i < 4)
			{
				*ptr = *(typebytes + i);
			}
			// copy args
			else if (i > 3 && i < bytelen)
			{
				char* argptr = arg + i - 4;
				*ptr = *argptr;
			}
			// copy terminator
			else if (i == bytelen - 1)
			{
				*ptr = PACKET_TERMINATOR;
			}
		}

		return bytes;
	}

	int type;
	char* arg;
	bool complete;
};
