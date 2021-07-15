#pragma once
#include <string>
#include <vector>

class Tokenizer
{
public:
	static void compileTypes(std::vector<std::string>* files);
	static void compileBodies();
private:
	explicit Tokenizer() {}
};

