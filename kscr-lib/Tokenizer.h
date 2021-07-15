#pragma once
#include <string>
#include <vector>

#include "Token.h"

class Tokenizer
{
public:
	static void tokenize(std::vector<std::string>* files);
	void digest(char data[]);
	const std::vector<Token> output = std::vector<Token>();
private:
	explicit Tokenizer() {}
};

