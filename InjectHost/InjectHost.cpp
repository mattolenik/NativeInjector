// InjectHost.cpp : Defines the entry point for the console application.

#include "stdafx.h"

int main()
{
    _setmode(_fileno(stdout), _O_WTEXT);
    HANDLE mutex = CreateMutex(nullptr, FALSE, _T("WinstonInjectHostMutex"));
    if(mutex == nullptr)
    {
        wprintf_s(_T("CreateMutex error: %d\n"), GetLastError());
        return 1;
    }
    WaitForSingleObject(mutex, INFINITE);
    Sleep(4000);
    LPTSTR buf = new TCHAR[1024];
    GetEnvironmentVariable(_T("TEST"), buf, 1024);
    wprintf_s(_T("%s"), buf);
    ReleaseMutex(mutex);
    return 0;
}