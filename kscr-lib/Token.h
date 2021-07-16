#pragma once
#include <sstream>
#include <string>
#include <utility>
#include <vector>

#include "Modifier.h"

enum class TokenType
{
	Nop   = 0x00,
	Op    = 0x01,
	Mod   = 0x02,
	Ident = 0x04,
	Num   = 0x08,
	Str   = 0x0F
};

struct Token
{
private:
	explicit Token(int type, int modifier, std::string arg) : type(type), modifier(modifier), arg(std::move(arg)) {}
public:
	explicit Token(Modifier modifier) : type(static_cast<int>(TokenType::Mod)), modifier(static_cast<int>(modifier)) {}
	explicit Token(TokenType type, std::string arg) : type(static_cast<int>(type)), modifier(static_cast<int>(Modifier::None)), arg(std::move(arg)) {}
	const int type;
	const int modifier;
	const std::string arg;

	int* bytes()
	{
		std::vector<int> buf = std::vector<int>();
		buf.push_back(type);
		buf.push_back(modifier);
		buf.push_back(arg.length());
		std::stringstream iss(arg);
		int number;
		while (iss >> number)
			buf.push_back(number);
		const auto out = buf.data();
		return out;
	}

	static Token Find(Token* prev, std::string key)
	{
		
	}

	static Token read(const int* bytes)
	{
		const int type = *bytes,
		modifier = *bytes,
		len = *bytes;
		char* argc = new char[len];
		for (int i = 0; i < len; i++)
			argc[i] = *bytes;
		return Token(type, modifier, std::string(argc));
	}
};
