#include <filesystem>
#include <iostream>
#include <regex>

using std::string;
using std::cout;
using std::endl;

namespace fs = std::filesystem;

#ifdef _WIN32
const string os_pathSep(";");
#else
const string os_pathSep(":");
#endif
const std::regex pathSepRgx(os_pathSep);

fs::path* sdkpath;

#pragma warning(disable : 4996)

fs::path* downloadSDK()
{
    throw std::exception("Downloading KSCR SDK is currently unsupported");
}

fs::path* findSDK()
{
    char* path = std::getenv("KSCR_HOME");
    if (path == nullptr)
    {
        // search in PATH
        string env(std::getenv("PATH"));
        std::sregex_token_iterator iter(
            env.begin(),
            env.end(),
            pathSepRgx,
            -1);
        std::sregex_token_iterator end;
        for (; iter != end; ++iter)
        {
            string res = *iter;
            fs::path here(res);
            if (exists((here /= "kscr.exe")))
                return new fs::path(absolute(here));
        }
    }
    if (path == nullptr)
        return downloadSDK();
    return new fs::path(string(path));
}

void runModules() {}
void runBinaries() {}

int main(int argc, char* argv[])
{
    sdkpath = findSDK();
    sdkpath->remove_filename();
    
    fs::path path;
    if (argc == 1)
    {
        path = fs::current_path();
        path = path.remove_filename();
    }
    else path = fs::path(argv[1]);

    if (exists(path / "module.kmod.json") || exists(path / "modules.kmod.json"))
        runModules();
    else
    {
        for (const auto& entry : fs::directory_iterator(path))
            std::cout << entry.path() << std::endl;
        runBinaries();
    }

    return 0;
}
