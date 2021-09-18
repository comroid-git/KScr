#pragma once
#include <map>

class NameEntry
{
public:
	static unsigned long next_id;
	explicit NameEntry(const char k) : key(k)
	{
		id = next_id++;
	}
	char key;
	unsigned long id;
	std::map<const char, NameEntry> sub = std::map<const char, NameEntry>();
	NameEntry* access(char* ptr, int rem)
	{
		if (rem >= 0)
		{
			char* np = ptr + 1;
			const char n = *np;
			if (sub.count(n) == 0)
				return (sub.at(n) = NameEntry(n)).access(np, rem - 1);
			return sub.at(n).access(np, rem - 1);
		} else if (rem == 0 && key == *ptr)
		{
			return this;
		}
		return nullptr;
	}
};

class NameCache
{
public:
	explicit NameCache() = default;
	NameEntry* operator[](std::string key)
	{
		return this->operator[](std::move(key));
	}
	NameEntry* operator[](char* key)
	{
		return root.access(key, sizeof key);
	}
private:
	NameEntry root = NameEntry('#');
};
