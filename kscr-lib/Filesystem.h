#pragma once
#include <string>

class Filesystem
{
public:
	static bool exists(std::string path);
	static bool isFile(std::string path);
	static bool isDir(std::string path);
};

