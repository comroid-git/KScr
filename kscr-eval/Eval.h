﻿#include <iostream>
#include <string>
#include <vector>
#include <regex>

#include "BytecodePacket.h"
#include "Token.h"

#pragma once

class Eval
{
public:
	static const std::vector<Token> tokenize(const char* sourcecode);
	static const BytecodePacket compile(const std::vector<Token>* tokens);
	static const int execute(const BytecodePacket* bytecode);
};
