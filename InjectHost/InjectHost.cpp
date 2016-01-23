// InjectHost.cpp : Defines the entry point for the console application.

#include "stdafx.h"
#include <string>
#include <iostream>

int main()
{
    _setmode(_fileno(stdout), _O_WTEXT);
    wchar_t buf[_MAX_ENV];
    std::wstring line;
    std::getline(std::wcin, line);
    if (line == L"continue")
    {
        DWORD count = GetEnvironmentVariable(L"PATH", buf, _MAX_ENV);
        DWORD error = GetLastError();
        std::wcout << count << std::endl;
        std::wcout << error << std::endl;
        std::wcout << buf;
        return 0;
    }
    std::wcout << "error" << std::endl;
    return 1;
}