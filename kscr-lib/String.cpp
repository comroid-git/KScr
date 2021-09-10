#include "pch.h"
#include "String.h"
#include <map>

static std::map<const char*, String*> cache = std::map<const char*, String*>();

String* String::instance(const char* bytes)
{
	String* ptr = cache[bytes];
	if (ptr == nullptr)
	{
		String str = String(bytes);
		return cache[bytes] = &str;
	}
	return ptr;
}
