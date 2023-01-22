#include <filesystem>
#include <iostream>

using std::string;
using std::cout;
using std::endl;

namespace fs = std::filesystem;

int main(int argc, char* argv[])
{
    fs::path path;
    if (argc == 1)
        path = fs::current_path().remove_filename();
    else path = fs::path(argv[1]);

    cout << absolute(path) << endl;
    
    return 0;
}
