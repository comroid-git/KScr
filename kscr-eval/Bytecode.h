#pragma once
#include <vector>

#include "BytecodePacket.h"

class Bytecode
{
public:
	std::vector<BytecodePacket> output = std::vector<BytecodePacket>();
	std::vector<BytecodePacket> extra = std::vector<BytecodePacket>();
};
