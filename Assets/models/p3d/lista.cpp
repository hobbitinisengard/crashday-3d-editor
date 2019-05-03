
#include "stdafx.h"
#include <string>
#include <iostream>
#include <filesystem>
namespace fs = std::experimental::filesystem;

int main()
{
    std::string path = "C:\Users\Kuba\Desktop\dat006\models";
    for (auto & p : fs::directory_iterator(path))
        std::cout << p << std::endl;
	return 0;
}
