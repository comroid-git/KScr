#pragma once
#include <set>
#include <string>
#include <vector>

class Filesystem
{
public:
	static bool exists(std::string path);
	static bool isFile(std::string path);
	static bool isDir(std::string path);
	static std::string simpleFileName(std::string path);
	static std::vector<std::string> splitpath(const std::string& str, std::set<char> delimiters);
};

