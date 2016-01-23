#include "stdafx.h"
#include <tchar.h>
#include <string>
#include "WinstonEnvUpdate.h"

#define MAX_ENV _MAX_ENV

void trimTrailingChar(LPWSTR str, WCHAR chr)
{
    size_t len = wcslen(str);
    if (str[len - 1] == chr) str[len - 1] = '\0';
}

void log(LPWSTR str)
{
#ifdef _DEBUG
    HANDLE file = CreateFile(L"D:\\log.txt", FILE_APPEND_DATA, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    DWORD written;
    std::wstring data(str);
    data += L"\r\n";
    auto bytes = data.c_str();
    WriteFile(file, bytes, data.length() * 2, &written, nullptr);
    CloseHandle(file);
#endif
}

void log(DWORD word, int radix = 10)
{
#ifdef _DEBUG
    WCHAR str[20];
    _itow_s(word, str, radix);
    log(str);
#endif
}

void log(bool val)
{
#ifdef _DEBUG
    if (val) {
        log(L"true");
    }
    else {
        log(L"false");
    }
#endif
}

void PrependPath(LPWSTR pathToAdd)
{
    log(pathToAdd);
    WCHAR path[MAX_ENV];
    DWORD copied = GetEnvironmentVariable(L"PATH", path, MAX_ENV);
    DWORD error = GetLastError();
    log(path);
    log(copied);
    log(error, 16);
    // Rare case of unset or empty variable
    if (copied == 0 || error == ERROR_ENVVAR_NOT_FOUND)
    {
        log(L"not copied or not found");
        SetEnvironmentVariable(L"PATH", pathToAdd);
        return;
    }

    LPWSTR nextToken = nullptr;
    wchar_t delims[] = L";";

    WCHAR pathToAddFull[MAX_PATH];
    _wfullpath(pathToAddFull, pathToAdd, MAX_PATH);
    trimTrailingChar(pathToAddFull, L'\\');
    log(pathToAddFull);

    LPWSTR token = nullptr;
    WCHAR tokenFull[MAX_PATH];
    WCHAR pathCopy[MAX_ENV];
    wcscpy_s(pathCopy, path);
    LPWSTR tokPtr = pathCopy;
    while ((token = wcstok_s(tokPtr, delims, &nextToken)) != nullptr)
    {
        tokPtr = nullptr;
        // Normalize all paths to full paths before making comparison
        // This makes sure equivalent paths with different representations
        // don't get duplicated (e.g. C:\path\to\foo and C:\path\to\..\to\foo)
        _wfullpath(tokenFull, token, MAX_PATH);
        trimTrailingChar(tokenFull, L'\\');
        if (lstrcmpi(pathToAddFull, tokenFull) == 0)
        {
            return;
        }
    }
    WCHAR newPath[MAX_ENV];
    newPath[0] = '\0';
    wcscat_s(newPath, MAX_ENV, pathToAddFull);
    wcscat_s(newPath, MAX_ENV, L";");
    wcscat_s(newPath, MAX_ENV, path);
    bool success = SetEnvironmentVariable(L"PATH", newPath);
    error = GetLastError();
    log(success);
    log(error, 16);
    log(newPath);
    //wprintf(L"%s\n"), newPath);
}

BOOL UpdateEnv()
{
    HANDLE mapFile = nullptr;
    WinstonEnvUpdate* env = nullptr;

    __try
    {
        mapFile = OpenFileMapping(FILE_MAP_READ, FALSE, SHARED_MEM_NAME);

        if (mapFile == nullptr)
        {
            log(L"Error opening file map");
            return FALSE;
        }

        env = static_cast<WinstonEnvUpdate*>(MapViewOfFile(mapFile, FILE_MAP_READ, 0, 0, sizeof(WinstonEnvUpdate)));

        if (env == nullptr)
        {
            log(L"Error opening shared memory");
            return FALSE;
        }

        if (wcscmp(env->operation, L"prepend") == 0)
        {
            PrependPath(env->path);
        }
        else
        {
            log(L"Unrecognized operation");
        }
    }
    __finally
    {
        if (env) UnmapViewOfFile(env);
        if (mapFile) CloseHandle(mapFile);
    }
    return TRUE;
}

BOOL APIENTRY DllMain(
    HMODULE hModule,
    DWORD ul_reason_for_call,
    LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    {
        TCHAR buf[MAX_PATH] = { 0 };
        _stprintf_s(buf, L"Attached process: %d", GetCurrentProcessId());
        log(buf);
        UpdateEnv();
        break;
    }

    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}