#include <filesystem>
#include <iostream>
#include <regex>
#include <Windows.h>

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

fs::path* sdkpath, path;

#pragma warning(disable : 4996)

fs::path* downloadSDK()
{
    throw std::exception("Downloading KSCR SDK is currently unsupported");
}

fs::path* findSDK()
{
    char* found = std::getenv("KSCR_HOME");
    if (found == nullptr)
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
    if (found == nullptr)
        return downloadSDK();
    return new fs::path(string(found));
}

void runModules()
{
    cout << "Compiling and executing module " << absolute(path);
}
void runBinaries()
{
    cout << "Running binaries in directory " << absolute(path);
}

int main(int argc, char* argv[])
{
#if _WIN32
    // do not show console window (may not work right)
    ::ShowWindow(::GetConsoleWindow(), SW_HIDE);
#endif
    
    sdkpath = findSDK();
    sdkpath->remove_filename();
    cout << "SDK Path found: " << absolute(*sdkpath);
    
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
