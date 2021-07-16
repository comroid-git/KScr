#pragma once
#include <string>
#include <vector>

#include "Token.h"

class Tokenizer
{
public:
	static void tokenize(std::vector<std::string>* files);
	std::vector<Token*> output = std::vector<Token*>();
	void digest(char data[]);
private:
	explicit Tokenizer() {}
};

