// InjectHost.cpp : Defines the entry point for the console application.

#include "stdafx.h"
#include <string>
#include <iostream>

int main()
{
    _setmode(_fileno(stdout), _O_WTEXT);

    std::wstring line;
    wchar_t buf[1024];
    std::getline(std::wcin, line);
    if (line == L"continue")
    {
        GetEnvironmentVariable(L"PATH", buf, 1024);
        std::wcout << buf;
        return 0;
    }
    return 1;
}