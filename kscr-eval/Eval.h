#include <iostream>
#include <string>
#include <vector>
#include <regex>

#include "Token.h"

#pragma once

class Eval
{
public:
	static const char* tokenize(const char* code);
	static const int execute(const char* code);
};
