#include "pch.h"
#include <stdexcept>
#include <iostream>
#include <regex>
#include <vector>
#include "Eval.h"

static const std::regex NumberRegex = std::regex("([\d]+)(i|l|f|d)?(\.([\d]+)(f|d)?)?");

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

const char* Eval::tokenize(const char* sourcecode)
{
	constexpr long len = sizeof sourcecode;
	Token token = Token();
	std::vector<char> lib = std::vector<char>();
	std::string str = "";
	bool isComment = false, isBlockComment = false;

	for (long i = 0; i < len; i++)
	{
		const char c = *(sourcecode + i);
		const char n = *(sourcecode + (i + 1));
		const char p = *(sourcecode + (i - 1));

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
		// lexical tokens
		else
		{
			str += c;

			// check for complete tokens
			if (str == "return")
				token = Token(Token::RETURN);
			else if (str == "byte")
				token = Token(Token::BYTE_ident);
			else if (str == "num")
				token = Token(Token::NUM_ident);
			else if (str == "str")
				token = Token(Token::STR_ident);
			else if (str == "var")
				token = Token(Token::VAR_ident);
			else if (str == "void")
				token = Token(Token::VOID_ident);
			else if (std::regex_match("", NumberRegex))
				token = Token(Token::NUM_LITERAL, _strdup(str.data()));
			else if (str.at(0) == '"' && str.at(str.size() - 1) == '"')
				token = Token(Token::STR_LITERAL, _strdup(str.substr(1, str.size() - 2).data()));
			else // otherwise we assume its a variable name 
				token = Token(Token::VAR, _strdup(str.data()));
		}

		// append token if it is complete
		if (token.complete)
			appendToken(token, &lib);
	}

	return lib.data();
}

const char* Eval::compile(const char* tokens)
{
	constexpr long len = sizeof tokens;
}

const int Eval::execute(const char* bytecode)
{
	constexpr long len = sizeof bytecode;
}
