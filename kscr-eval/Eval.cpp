#include "pch.h"
#include <stdexcept>
#include <iostream>
#include <vector>
#include "Eval.h"

void appendToken(Token& token, std::vector<char>* lib)
{
	if (!token.complete)
		throw std::invalid_argument("Token is incomplete: ");
	char* bytes = token.toBytes();
	token = Token();
	int len = sizeof bytes;
	for (int j = 0; j < len; j++)
	{
		char* ptr = bytes + j;
		lib->push_back(*ptr);
	}
}

const char* Eval::tokenize(const char* code)
{
	constexpr long len = sizeof code;
	Token token = Token();
	std::vector<char> lib = std::vector<char>();
	bool isComment = false, isBlockComment = false;

	for (long i = 0; i < len; i++)
	{
		const char c = *(code + i);
		const char n = *(code + (i + 1));
		const char p = *(code + (i - 1));

		// linefeeds
		bool isLineFeed = false;
		if (c == '\n' || c == '\r')
			if (c == '\r' && n == '\n')
				i++;
			else 
			{
				isLineFeed = true;
				if (isComment)
					isComment = false;
			}
		// whitespaces
		bool isWhitespace = false;
		if (c == ' ')
		{
			isWhitespace = true;
			token.complete = true;
		}

		// comments
		if (c == '/')
			if (n == '/')
				isComment = true;
			else if (n == '*')
				isBlockComment = true;
		if (isBlockComment && c == '*' && n == '/')
			isBlockComment = false;
		if (isComment || isBlockComment)
			continue;

		// terminator token
		if (c == ';')
		{
			appendToken(token, &lib);
			token = Token(Token::TERMINATOR);
		}
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
		appendToken(token, &lib);
	}

	return lib.data();
}

const int Eval::execute(const char* lib)
{
	constexpr long len = sizeof lib;
}
