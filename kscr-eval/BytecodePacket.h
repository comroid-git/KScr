#pragma once
#include "Token.h"

class BytecodePacket
{
public:
	// packet bytes definitions
	static constexpr char PACKET_TERMINATOR = 0xFFFF;

	explicit BytecodePacket(Token* token, BytecodePacket* followupPacket) : token(token), followupPacket(followupPacket) {}

	Token* token;
	BytecodePacket* followupPacket;
};
