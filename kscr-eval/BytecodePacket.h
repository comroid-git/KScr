#pragma once

class BytecodePacket
{
public:
	// packet bytes definitions
	static constexpr char PACKET_TERMINATOR  = 0xFFFF;

	// base types
	static constexpr int DECLARATION         = 0x0001;
	static constexpr int ASSIGNMENT          = 0x0002;
	static constexpr int STATEMENT           = 0x0004;
	static constexpr int EXPRESSION          = 0x0008;
	static constexpr int METHOD_CALL         = 0x000F;

	// static declaration args
	static constexpr int DECLARATION_BYTE    = 0x0010;
	static constexpr int DECLARATION_NUMERIC = 0x0020;
	static constexpr int DECLARATION_STRING  = 0x0040;
	static constexpr int DECLARATION_VAR     = 0x0080;
	static constexpr int DECLARATION_VOID    = 0x00F0;

	// static assignment args
	static constexpr int ASSIGNMENT_NUMERIC    = 0x0010;
	static constexpr int ASSIGNMENT_STRING     = 0x0020;
	static constexpr int ASSIGNMENT_TRUE       = 0x0040;
	static constexpr int ASSIGNMENT_FALSE      = 0x0080;
	static constexpr int ASSIGNMENT_EXPRESSION = 0x00F0;

	// static statement args

	// static expression args

	// static method call args

	// class structure
	explicit BytecodePacket() = default;

	int type = 0;
	char* arg = nullptr;
	BytecodePacket* previousPacket = nullptr;
	BytecodePacket* followupPacket = nullptr;
	bool complete = false;
};
