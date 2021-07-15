#pragma once
#include <string>

class Filesystem
{
public:
	static bool exists(const std::string path);
	static bool isFile(const std::string path);
	static bool isDir(const std::string path);
};

