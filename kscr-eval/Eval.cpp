#include "pch.h"
#include <stdexcept>
#include <iostream>
#include <regex>
#include <vector>
#include "Eval.h"

#include "BytecodePacket.h"
#include "../kscr-lib/Numeric.h"

std::map<unsigned long, void*> obj_map = std::map<unsigned long, void*>();

void appendToken(Token* token, std::vector<Token>* lib, std::string* str)
{
	if (!token->complete)
		return;// throw std::invalid_argument("Token is incomplete: ");
	lib->push_back(*token);
	*token = Token();
	*str = "";
}

const std::vector<Token> Eval::tokenize(const char* sourcecode, const int len)
{
	Token token = Token();
	std::vector<Token> lib = std::vector<Token>();
	std::string str;
	bool isComment = false, isBlockComment = false;

	for (long i = 0; i < len; i++)
	{
		const char c = *(sourcecode + i);
		const char n = i + 1 < len ? *(sourcecode + (i + 1)) : ' ';
		const char p = i - 1 > 0 ? *(sourcecode + (i - 1)) : ' ';

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
			token = Token(Token::TERMINATOR, _strdup(str.data()));
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
			if (!isWhitespace)
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
			else if (std::regex_match(str, Numeric::NumberRegex) && (n == ';' || n == ' '))
				token = Token(Token::NUM_LITERAL, _strdup(str.data()));
			else if (str.size() > 2 && str.at(0) == '"' && str.at(str.size() - 1) == '"' && (n == ';' || n == ' '))
				token = Token(Token::STR_LITERAL, _strdup(str.substr(1, str.size() - 2).data()));
			else if (str == "true")
				token = Token(Token::TRUE);
			else if (str == "false")
				token = Token(Token::FALSE);
			else if ((isWhitespace || isLineFeed) && !str.empty())
				token = Token(Token::VAR, _strdup(str.data()));
		}

		// append token if it is complete
		if (token.complete && token.type != 0)
			appendToken(&token, &lib, &str);
	}

	return lib;
}

Bytecode output;
int eIndex = -1;
int index = -1;
BytecodePacket* packet = nullptr;
BytecodePacket* prevPacket = nullptr;
int nextIntoAlt = -1;
int nextIntoSub = -1;

void pushPacket()
{
	// todo some pointers are incorrect
	if (nextIntoAlt == 0)
		prevPacket->altPacketEindex = eIndex;
	else if (nextIntoSub == 0)
		prevPacket->subPacketEindex = eIndex;
	if (nextIntoAlt == 1 || nextIntoSub == 1)
	{
		output.extra.emplace_back(BytecodePacket());
		eIndex++;
		prevPacket = packet;
		packet = &output.extra.at(eIndex);
	}
	else
	{
		output.output.emplace_back(BytecodePacket());
		index++;
		packet = &output.output.at(index);
		int prevIndex = index - 1;
		if (prevIndex < static_cast<int>(output.output.size()) && prevIndex >= 0)
			prevPacket = &output.output.at(index - 1);
		else prevPacket = nullptr;
	}

	if (nextIntoAlt >= 0)
		nextIntoAlt--;
	if (nextIntoSub >= 0)
		nextIntoSub--;
}

const Bytecode* Eval::compile(const std::vector<Token>* tokens)
{
	output = Bytecode();
	
	const long len = tokens->size();
	pushPacket();

	for (int i = 0; i < len; i++)
	{
		// this & next token
		const Token* token = &tokens->at(i);
		const Token* next = static_cast<int>(tokens->size()) - 1 > i ? &tokens->at(i + 1) : nullptr;

		// terminator
		if (token->type == Token::TERMINATOR)
		{
			//todo do any statement finalizing here
			pushPacket();
		}
		// declarations
		else if (token->type == Token::BYTE_ident)
		{
			if (next->type != Token::VAR)
				throw std::exception("Invalid byte assignment: Missing variable name");
			packet->type = BytecodePacket::DECLARATION_BYTE;
			packet->arg = next->arg; // var name
			pushPacket();
		}
		else if (token->type == Token::NUM_ident)
		{
			if (next->type != Token::VAR)
				throw std::exception("Invalid numeric assignment: Missing variable name");
			packet->type = BytecodePacket::DECLARATION_NUMERIC;
			packet->arg = next->arg; // var name
			pushPacket();
		}
		else if (token->type == Token::STR_ident)
		{
			if (next->type != Token::VAR)
				throw std::exception("Invalid string assignment: Missing variable name");
			packet->type = BytecodePacket::DECLARATION_STRING;
			packet->arg = next->arg; // var name
			pushPacket();
		}
		else if (token->type == Token::VAR_ident)
		{
			if (next->type != Token::VAR)
				throw std::exception("Invalid var assignment: Missing variable name");
			packet->type = BytecodePacket::DECLARATION_VARIABLE;
			packet->arg = next->arg; // var name
			pushPacket();
		}
		else if (token->type == Token::VOID_ident)
		{
			if (next == nullptr && next->type != Token::VAR)
				throw std::exception("Invalid void assignment: Missing variable name");
			packet->type = BytecodePacket::DECLARATION_VOID;
			packet->arg = next->arg; // var name
			pushPacket();
		}
		// syntax operators
		// = symbol
		else if (token->type == Token::EQUALS)
		{
			// todo: handle equals
			// assignments
			if (((prevPacket->type & BytecodePacket::DECLARATION) != 0 || (prevPacket->type & BytecodePacket::EXPRESSION_VAR) != 0)
				&& next != nullptr && (next->type == Token::VAR || next->type == Token::STR_LITERAL || next->type == Token::NUM_LITERAL))
				// if previous is a declaration or variable & next is varname or literal
			{
				packet->type = BytecodePacket::ASSIGNMENT;
				nextIntoSub = 1;
				pushPacket();
			}
		}
		// + symbol
		else if (token->type == Token::PLUS)
		{
			packet->type = BytecodePacket::OPERATOR_PLUS;
			nextIntoAlt = 1;
			pushPacket();
		}
		// - symbol
		else if (token->type == Token::MINUS)
		{
			packet->type = BytecodePacket::OPERATOR_MINUS;
			nextIntoAlt = 1;
			pushPacket();
		}
		// * symbol
		else if (token->type == Token::MULTIPLY)
		{
			packet->type = BytecodePacket::OPERATOR_MULTIPLY;
			nextIntoAlt = 1;
			pushPacket();
		}
		// / symbol
		else if (token->type == Token::DIVIDE)
		{
			packet->type = BytecodePacket::OPERATOR_DIVIDE;
			nextIntoAlt = 1;
			pushPacket();
		}
		// % symbol
		else if (token->type == Token::MODULUS)
		{
			packet->type = BytecodePacket::OPERATOR_MODULUS;
			nextIntoAlt = 1;
			pushPacket();
		}

		// expressions
		// numeric literal
		else if (token->type == Token::NUM_LITERAL)
		{
			packet->type = BytecodePacket::LITERAL_NUMERIC;
			packet->arg = Numeric::parse(token->arg);
			pushPacket();
		}
		// string literal
		else if (token->type == Token::STR_LITERAL)
		{
			packet->type = BytecodePacket::LITERAL_STRING;
			packet->arg = token->arg;
			pushPacket();
		}
		// boolean literals
		else if (token->type == Token::TRUE || token->type == Token::FALSE)
		{
			packet->type = (token->type == Token::TRUE ? BytecodePacket::LITERAL_TRUE : BytecodePacket::LITERAL_FALSE);
			pushPacket();
		}
		// var names
		else if (token->type == Token::VAR)
		{
			packet->type = BytecodePacket::EXPRESSION_VAR;
			packet->arg = token->arg;
			pushPacket();
		}

		/*todo
			// allow initialization at declaration
			// by moving the equals operator in declaration alt
			bool cond = (i + 2 < static_cast<int>(tokens->size()) && tokens->at(i + 2).type != Token::EQUALS);
			if ((packet.type & BytecodePacket::DECLARATION) != 0 && next->type == Token::EQUALS && cond)
				nextIntoAlt = 1;
			// and moving an expression into the prevAltPacket that is the equals
			if (prevAltPacket != nullptr && (prevAltPacket->type & BytecodePacket::ASSIGNMENT) != 0 && (packet.type & BytecodePacket::EXPRESSION) != 0)
				nextIntoAltAlt = 0;
			*/
	}

	return &output;

}

const int Eval::execute(const Bytecode* bytecode)
{
	void* yield = nullptr;

	const long len = static_cast<long>(bytecode->output.size());
	for (long i = 0; i < len; i++)
	{
		BytecodePacket it = bytecode->output.at(i);
		long previ = i - 1;
		const BytecodePacket* prev = previ < 0 ? nullptr : &bytecode->output.at(previ);

		yield = it.evaluate(bytecode, prev, yield, &obj_map);
	}

	if (yield == nullptr) {
		std::cerr << "Program exited without exit code";
		return 0;
	}
	return *static_cast<int*>(yield);
}
