#pragma once

class BytecodePacket
{
public:
	// packet bytes definitions
	static constexpr char PACKET_TERMINATOR = 0xFFFF;

	// base types
	static constexpr int DECLARATION        = 0x0001;
	static constexpr int ASSIGNMENT         = 0x0002;
	static constexpr int STATEMENT          = 0x0004;
	static constexpr int OPERATOR           = 0x0008;
	static constexpr int METHOD_CALL        = 0x000F;

	explicit BytecodePacket() : type(0), arg(nullptr), followupPacket(nullptr) {}
	explicit BytecodePacket(int type, char* arg) : type(type), arg(arg), followupPacket(nullptr) {}
	explicit BytecodePacket(int type, char* arg, BytecodePacket* followupPacket) : type(type), arg(arg), followupPacket(followupPacket), complete(true) {}

	int type;
	char* arg;
	BytecodePacket* followupPacket;
	bool complete = false;
};
