#include "pch.h"
#include <iostream>
#include <vector>
#include "Eval.h"

const char* Eval::tokenize(const char* code)
{
	constexpr long len = sizeof code;
	Token token = Token();
	std::vector<char> lib = std::vector<char>();

	for (long i = 0; i < len; i++)
	{
		const char c = *(code + i);
		const char n = *(code + (i + 1));
		const char p = *(code + (i - 1));

		bool isLineFeed = false;
		if (c == '\n' || c == '\r')
			c == '\r' && n == '\n' ? i++ : isLineFeed = true;

		// terminator token
		if (c == ';')
			token = Token(Token::TERMINATOR);
		// arithmetic tokens
		else if (c == '+')
			token = Token(Token::PLUS);
		else if (c == '-')
			token = Token(Token::MINUS);
		else if (c == '*')
			token = Token(Token::MULTIPLY);
		else if (c == '/')
			token = Token(Token::DIVIDE);
		else if (c == '%')
			token = Token(Token::MODULUS);

		// append token if it is complete
		if (!token.complete)
			continue;
		char* bytes = token.toBytes();
		token = Token();
		int len = sizeof bytes;
		for (int j = 0; j < len; j++)
		{
			char* ptr = bytes + j;
			lib.push_back(*ptr);
		}
	}

	return lib.data();
}

const int Eval::execute(const char* lib)
{
	constexpr long len = sizeof lib;
}
