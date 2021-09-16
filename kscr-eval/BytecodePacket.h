#pragma once
#include <map>
#include <string>
#include <memory>

#include "Bytecode.h"

class BytecodePacket
{
public:
	// packet bytes definitions
	static constexpr char PACKET_TERMINATOR  = 0xFFFF;

	// base types
	static constexpr int DECLARATION            = 0x00000001;
	static constexpr int ASSIGNMENT             = 0x00000002;
	static constexpr int EXPRESSION             = 0x00000004;
	static constexpr int STATEMENT              = 0x00000008;
	static constexpr int OPERATOR               = 0x0000000F | EXPRESSION;

	// static declaration types
	static constexpr int DECLARATION_BYTE       = 0x00000010 | DECLARATION;
	static constexpr int DECLARATION_NUMERIC    = 0x00000020 | DECLARATION;
	static constexpr int DECLARATION_STRING     = 0x00000040 | DECLARATION;
	static constexpr int DECLARATION_VARIABLE   = 0x00000080 | DECLARATION;
	static constexpr int DECLARATION_VOID       = 0x000000F0 | DECLARATION;

	// static assignment types
	static constexpr int LITERAL_NUMERIC        = 0x00000100 | EXPRESSION;
	static constexpr int LITERAL_STRING         = 0x00000200 | EXPRESSION;
	static constexpr int LITERAL_TRUE           = 0x00000400 | EXPRESSION;
	static constexpr int LITERAL_FALSE          = 0x00000800 | EXPRESSION;
	static constexpr int EXPRESSION_VAR         = 0x00000F00 | EXPRESSION;

	// static operator args
	static constexpr int OPERATOR_PLUS          = 0x00001000 | OPERATOR;
	static constexpr int OPERATOR_MINUS         = 0x00002000 | OPERATOR;
	static constexpr int OPERATOR_MULTIPLY      = 0x00004000 | OPERATOR;
	static constexpr int OPERATOR_DIVIDE        = 0x00008000 | OPERATOR;
	static constexpr int OPERATOR_MODULUS       = 0x0000F000 | OPERATOR;

	// static expression args

	// static method call args

	// class structure
	explicit BytecodePacket() = default;
	void* evaluate(const Bytecode* bytecode, const BytecodePacket* prev, void* prevResult, std::map<const char*, void*>* obj_map);

	int type = 0;
	void* arg = nullptr;
	int altPacketEindex = -1;
	int subPacketEindex = -1;
};
