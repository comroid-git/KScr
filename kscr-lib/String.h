#pragma once

class String
{
public:
	explicit String(const char* bytes) : bytes(bytes) {}
	const char* bytes;
	static String* instance(const char* bytes);
};
