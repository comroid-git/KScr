#include <iostream>
#include <string>
#include <vector>
#include <regex>

#include "Token.h"

#pragma once

class Eval
{
public:
	static const char* tokenize(const char* sourcecode);
	static const char* compile(const char* tokens);
	static const int execute(const char* bytecode);
};
