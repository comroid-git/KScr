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


void startup(LPCSTR lpApplicationName, string arg)
{
    // additional information
    STARTUPINFOA si;
    PROCESS_INFORMATION pi;

    // set the size of the structures
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));
    
    char* args = static_cast<char*>(malloc(0));
    strcpy(args, arg.c_str());

#if _DEBUG
    if (lpApplicationName != nullptr)
        cout << "Using executable " << lpApplicationName << endl;
#endif
    // start the program up
    auto success = CreateProcessA
    (
        lpApplicationName,   // the path
        args,                // Command line
        nullptr,                   // Process handle not inheritable
        nullptr,                   // Thread handle not inheritable
        TRUE,                  // Set handle inheritance to FALSE
        0,     // (Opens file in a separate console when 'CREATE_NEW_CONSOLE')
        nullptr,           // Use parent's environment block
        nullptr,           // Use parent's starting directory 
        &si,            // Pointer to STARTUPINFO structure
        &pi           // Pointer to PROCESS_INFORMATION structure
    );
    
    WaitForSingleObject(pi.hProcess, INFINITE);
    
    // Close process and thread handles. 
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);

    if (!success)
        throw std::exception("Could not start subprocess");
}
void runModules()
{
#if _DEBUG
    cout << "Compiling and executing module root " << absolute(path) << endl;
#endif
    startup(fs::absolute(*sdkpath / "kbuild.exe").string().c_str(), " run " + absolute(path).string()
#if !_DEBUG
        +" -q"
#endif
    );
}
void runBinaries()
{
#if _DEBUG
    cout << "Running binaries in directory " << absolute(path) << endl;
#endif
}

int main(int argc, char* argv[])
{
#if _WIN32 & !_DEBUG
    // do not show console window (may not work right)
    ::ShowWindow(::GetConsoleWindow(), SW_HIDE);
#endif
    
    sdkpath = findSDK();
    sdkpath->remove_filename();
#if _DEBUG
    cout << "SDK Path found: " << absolute(*sdkpath) << endl;
#endif
    
    path = argc == 1 ? fs::current_path() : fs::path(argv[1]);
    if (path.has_extension())
        path = path.remove_filename();

    try
    {
        if (exists(path / "module.kmod.json") || exists(path / "modules.kmod.json"))
            runModules();
        else
        {
            for (const auto& entry : fs::directory_iterator(path))
                std::cout << entry.path() << std::endl;
            runBinaries();
        }
    } catch (std::exception& e)
    {
        cout << "Internal error: " << e.what() << endl;
    }

    return 0;
}
