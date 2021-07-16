#pragma once
#include <sstream>
#include <string>
#include <regex>
#include <vector>

#include "Modifier.h"

enum class TokenType
{
	Nop   = 0x00,
	Op    = 0x01,
	Mod   = 0x02,
	Ident = 0x04,
	Num   = 0x08,
	Str   = 0x0F,
	Par   = 0x10
};

const std::regex numRegex = std::regex("\d+");

struct Token
{
private:
	explicit Token(int type, int modifier, std::string* arg) : type(type), modifier(modifier), arg(arg)
	{
	}

public:
	explicit Token(Modifier modifier) : type(static_cast<int>(TokenType::Mod)), modifier(static_cast<int>(modifier)), arg(nullptr)
	{
	}

	explicit Token(TokenType type, std::string* arg) : type(static_cast<int>(type)),
	                                                  modifier(static_cast<int>(Modifier::None)), arg(arg)
	{
	}

	const int type;
	const int modifier;
	const std::string* arg;

	int* bytes()
	{
		auto buf = std::vector<int>();
		buf.push_back(type);
		buf.push_back(modifier);
		buf.push_back(arg->length());
		std::stringstream iss(*arg);
		int number;
		while (iss >> number)
			buf.push_back(number);
		const auto out = buf.data();
		return out;
	}

	static Token Find(Token* prev, std::string key)
	{
		const int len = key.length();
		
		//region Modifiers
		if (key == "public")
			return Token(Modifier::Public);
		if (key == "protected")
			return Token(Modifier::Protected);
		if (key == "internal")
			return Token(Modifier::Internal);
		if (key == "private")
			return Token(Modifier::Private);
		if (key == "static")
			return Token(Modifier::Static);
		if (key == "abstract")
			return Token(Modifier::Abstract);
		if (key == "final")
			return Token(Modifier::Final);
		if (key == "class")
			return Token(Modifier::Class);
		if (key == "enum")
			return Token(Modifier::Enum);
		if (key == "interface")
			return Token(Modifier::Interface);
		if (key == "annotation")
			return Token(Modifier::Annotation);
		//endregion

		//region Operators
		if (key == "+" || key == "-" || key == "/" || key == "*" || key == "%" || key == "&" || key == "|" || key == "!" || key == "?")
			return Token(TokenType::Op, &key);
		//endregion

		//region Numbers
		if (std::regex_match(key, numRegex))
			return Token(TokenType::Num, &key);
		//endregion

		//region Strings
		char f = key.at(0);
		char l = key.at(len - 1);
		if (f == l == '"') {
			std::string str = key.substr(1, len- 2);
			return Token(TokenType::Str, &str);
		}
		//endregion

		//region Parentheses
		if (key == "(" || key == ")" || key == "[" || key == "]" || key == "{" || key == "}" || key == "<" || key == ">")
			return Token(TokenType::Par, &key);
		//endregion

		//region Parentheses
		if (key == ";")
			return Token(TokenType::Nop, &key);
		//endregion

		//region Identifiers
		// return as identifier by default
		return Token(TokenType::Ident, &key);
		//endregion
	}

	static Token read(const int* bytes)
	{
		const int type = *bytes,
		          modifier = *bytes,
		          len = *bytes;
		auto argc = new char[len];
		for (int i = 0; i < len; i++)
			argc[i] = *bytes;
		std::string str = std::string(argc);
		return Token(type, modifier, &str);
	}
};
