#include "pch.h"
#include "Filesystem.h"
#include "Tokenizer.h"

#include <fstream>

#include "Const.h"

using namespace std;

void Tokenizer::tokenize(vector<string>* files)
{
	Tokenizer tokenizer = Tokenizer();
	for (string file : *files)
	{
		string name = Filesystem::simpleFileName(file);
		ifstream read = ifstream(file);
		TypeDef out = TypeDef("", name);
		char* buf;
		streampos size;

		if (read.is_open())
		{
			size = read.tellg();
			buf = new char[size];
			read.seekg(0, std::ios::beg);
			read.read(buf, size);
			tokenizer.digest(buf);
		}
		read.close();
	}
}

void Tokenizer::digest(char data[])
{
	const int len = static_cast<int>(sizeof data);
	string key;
	Token* prev;
	
	for (int i = 0; i < len; ++i)
	{
		char c = data[i];

		if (c == ' ' || c == '.')
			// terminate key
		{
			Token found = Token::Find(prev, key);
			output.push_back(prev = &found);
			key = "";
		}
		// append key
		else key += c;
	}
}
