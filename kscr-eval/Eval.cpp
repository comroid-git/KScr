#include "pch.h"
#include "Eval.h"

const char* Eval::compile(const char* code)
{
	constexpr long len = sizeof code;

	for (long i = 0; i < len; i++)
	{
		const char c = *(code + i);
		const char n = *(code + (i + 1));
		const char p = *(code + (i - 1));

		bool isLineFeed = false;
		if (c == '\n' || c == '\r')
			c == '\r' && n == '\n' ? i++ : isLineFeed = true;
		
		if (isTermination)
	}
}

static std::vector<Token>

const int Eval::execute(const char* lib)
{
	constexpr long len = sizeof lib;
}
