#include "pch.h"
#include "Filesystem.h"
#include <sys/stat.h>
#include <string>
#include <fstream>
#include <set>
#include <vector>

bool Filesystem::exists(const std::string path)
{
	struct stat buffer;
	return (stat(path.c_str(), &buffer) == 0);
}

bool Filesystem::isFile(const std::string path)
{
	return false;
}

bool Filesystem::isDir(const std::string path)
{
	return false;
}

std::string Filesystem::simpleFileName(std::string path)
{
    return splitpath(path, { "/", "\\" }).back();
}

// https://stackoverflow.com/questions/8520560/get-a-file-name-from-a-path
std::vector<std::string> Filesystem::splitpath(const std::string& str, const std::set<char> delimiters)
{
    std::vector<std::string> result;

    char const* pch = str.c_str();
    char const* start = pch;
    for (; *pch; ++pch)
    {
        if (delimiters.find(*pch) != delimiters.end())
        {
            if (start != pch)
            {
                std::string str(start, pch);
                result.push_back(str);
            }
            else
            {
                result.push_back("");
            }
            start = pch + 1;
        }
    }
    result.push_back(start);

    return result;
}
