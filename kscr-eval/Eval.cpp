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
		throw std::exception("Token is incomplete: ");
	lib->push_back(*token);
	*token = Token();
}

const std::vector<Token> Eval::tokenize(const char* sourcecode)
{
	constexpr long len = sizeof *sourcecode;
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
		else if (c == '=') 
			token = Token(Token::EQUALS);
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

BytecodePacket* prevPacket = nullptr;
BytecodePacket packet = BytecodePacket();

void finalizePacket()
{
	if (!packet.complete)
		throw std::exception("Packet is incomplete");
	prevPacket = &packet;
	packet = BytecodePacket();
	prevPacket->followupPacket = &packet;
}

const BytecodePacket Eval::compile(const std::vector<Token>* tokens)
{
	constexpr long len = sizeof *tokens;

	for (int i = 0; i < len; i++)
	{
		// initialize new packet
		packet.previousPacket = prevPacket;

		// this & next token
		const Token* token = &tokens->at(i);
		const Token* next = &tokens->at(i + 1);

		// terminator
		if (token->type == Token::TERMINATOR)
			packet.complete = true;

		// declarations
		if (token->type == Token::BYTE_ident)
		{
			if (next->type != Token::VAR)
				throw std::exception("Invalid byte assignment: Missing variable name");
			packet.type |= BytecodePacket::DECLARATION_BYTE | BytecodePacket::DECLARATION;
			packet.arg = next->arg; // var name
			packet.complete = true;
		}
		else if (token->type == Token::NUM_ident)
		{
			if (next->type != Token::VAR)
				throw std::exception("Invalid numeric assignment: Missing variable name");
			packet.type |= BytecodePacket::DECLARATION_NUMERIC | BytecodePacket::DECLARATION;
			packet.arg = next->arg; // var name
			packet.complete = true;
		}
		else if (token->type == Token::STR_ident)
		{
			if (next->type != Token::VAR)
				throw std::exception("Invalid string assignment: Missing variable name");
			packet.type |= BytecodePacket::DECLARATION_STRING | BytecodePacket::DECLARATION;
			packet.arg = next->arg; // var name
			packet.complete = true;
		}
		else if (token->type == Token::VAR_ident)
		{
			if (next->type != Token::VAR)
				throw std::exception("Invalid var assignment: Missing variable name");
			packet.type |= BytecodePacket::DECLARATION_VAR | BytecodePacket::DECLARATION;
			packet.arg = next->arg; // var name
			packet.complete = true;
		}
		else if (token->type == Token::VOID_ident)
		{
			if (next->type != Token::VAR)
				throw std::exception("Invalid void assignment: Missing variable name");
			packet.type |= BytecodePacket::DECLARATION_VOID | BytecodePacket::DECLARATION;
			packet.arg = next->arg; // var name
			packet.complete = true;
		}

		// assignments
		// number assignment
		else if (token->type == Token::EQUALS && next->type == Token::NUM_LITERAL)
		{
			packet.type |= BytecodePacket::ASSIGNMENT | BytecodePacket::ASSIGNMENT_NUMERIC;
			packet.arg = next->arg;
			packet.complete = true;
		}
		else if (token->type == Token::EQUALS && next->type == Token::STR_LITERAL)
		{
			packet.type |= BytecodePacket::ASSIGNMENT | BytecodePacket::ASSIGNMENT_STRING;
			packet.arg = next->arg;
			packet.complete = true;
		}
		// todo: boolean assignments & expression assignments

		// finalize packet if complete
		if (packet.complete)
		{
			finalizePacket();
		}
	}

	return packet;

}

const int Eval::execute(const std::vector<BytecodePacket>* bytecode)
{
	constexpr long len = sizeof *bytecode;
}
