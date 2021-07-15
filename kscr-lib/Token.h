#pragma once
#include <string>
#include <utility>
#include <vector>

#include "Modifier.h"

enum class TokenType
{
	Nop = 0x0,
	Op = 0x1,
	Mod = 0x2,
	Ident = 0x4,
	Num = 0x8,
	Str = 0xF
};

struct Token
{
	explicit Token(TokenType type, Modifier modifier) : type(type), modifier(modifier) {}
	explicit Token(TokenType type, Modifier modifier, std::string arg) : type(type), modifier(modifier), arg(std::move(arg)) {}
	const TokenType type;
	const Modifier modifier;
	const std::string arg;
	const std::vector<Token> sub = std::vector<Token>();
};
