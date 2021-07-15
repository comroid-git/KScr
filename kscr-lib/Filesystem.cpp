#include "pch.h"
#include "Filesystem.h"
#include <sys/stat.h>
#include <string>
#include <fstream>

static bool exists(const std::string path)
{
	struct stat buffer;
	return (stat(path.c_str(), &buffer) == 0);
}

static bool isFile(const std::string path)
{
	return false;
}

static bool isDir(const std::string path)
{
	return false;
}
