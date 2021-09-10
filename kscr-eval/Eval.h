#include <iostream>
#include <string>
#include <vector>

#pragma once

class Eval
{
public:
	static const char* compile(const char* code);
	static const int execute(const char* code);
};
