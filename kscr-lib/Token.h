#pragma once
#include <string>
#include <utility>
#include <vector>

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
	explicit Token(TokenType type) : type(type) {}
	explicit Token(TokenType type, std::string arg) : type(type), arg(std::move(arg)) {}
	const TokenType type;
	const std::string arg;
	const std::vector<Token> sub = std::vector<Token>();
};
