#pragma once
#include <iostream>

class Token
{
public:
	// token class definitions
	static const char PACKET_TERMINATOR = 0xFFFF;

	// basic symbols
	static const int TERMINATOR = 0x0000; // ';' statement termination
	static const int VAR        = 0x0001; // variable identifier
	static const int VAR_ident  = 0x0002; // 'var'-declaration
	static const int NUM_ident  = 0x0004; // 'num'-declaration
	static const int BYTE_ident = 0x0008; // 'byte'-declaration
	static const int STR_ident  = 0x000F; // 'str'-declaration
	
	static const int VOID_ident = 0x0010;
	static const int reserved2  = 0x0020;
	static const int reserved3  = 0x0040;
	static const int reserved4  = 0x0080;
	static const int RETURN     = 0x00F0; // 'return'-statement

	// arithmetic operators
	static const int PLUS       = 0x0100; // + symbol
	static const int MINUS      = 0x0200; // - symbol
	static const int MULTIPLY   = 0x0400; // * symbol
	static const int DIVIDE     = 0x0800; // / symbol
	static const int MODULUS    = 0x0F00; // % symbol

	explicit Token(int type, char* arg) : type(type), arg(arg) {}

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

private:
	int type;
	char* arg;
};
