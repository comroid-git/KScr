#include <iostream>
#include <string>
#include <vector>
#include <regex>

#include "Bytecode.h"
#include "Token.h"

#pragma once

class Eval
{
public:
	static const std::vector<Token> tokenize(const char* sourcecode, const int len);
	static const Bytecode* compile(const std::vector<Token>* tokens);
	static const int execute(const Bytecode* bytecode);
};
