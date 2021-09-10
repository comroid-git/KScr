#include "pch.h"
#include <stdexcept>
#include <iostream>
#include <regex>
#include <vector>
#include "Eval.h"

static const std::regex NumberRegex = std::regex("([\d]+)(i|l|f|d)?(\.([\d]+)(f|d)?)?");

void appendToken(Token* token, std::vector<Token>* lib)
{
	if (!token->complete)
		throw std::invalid_argument("Token is incomplete: ");
	lib->push_back(*token);
	*token = Token();
}

const std::vector<Token> Eval::tokenize(const char* sourcecode)
{
	constexpr long len = sizeof sourcecode;
	Token token = Token();
	std::vector<Token> lib = std::vector<Token>();
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
			appendToken(&token, &lib);
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
			bool prevcomplete = token.complete;
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

			// reset string if token is now completed
			if (!prevcomplete && token.complete)
				str = "";
		}

		// append token if it is complete
		if (token.complete)
			appendToken(&token, &lib);
	}

	return lib;
}

const std::vector<BytecodePacket> Eval::compile(const std::vector<Token>* tokens)
{
	constexpr long len = sizeof tokens;
}

const int Eval::execute(const std::vector<BytecodePacket>* bytecode)
{
	constexpr long len = sizeof bytecode;
}
