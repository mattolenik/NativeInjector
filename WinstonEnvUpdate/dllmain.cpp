#include "stdafx.h"
#include <tchar.h>
#include <string>
#include "WinstonEnvUpdate.h"

void trimTrailingChar(LPWSTR path, WCHAR chr)
{
    size_t len = wcslen(path);
    if (path[len - 1] == chr) path[len - 1] = '\0';
}

void PrependPath(LPWSTR pathToAdd)
{
    WCHAR path[_MAX_ENV];
    DWORD copied = GetEnvironmentVariable(L"PATH", path, _MAX_ENV);
    // Rare case of unset or empty variable
    if(copied == 0 || GetLastError() != NO_ERROR)
    {
        SetEnvironmentVariable(L"PATH", pathToAdd);
        return;
    }

    LPWSTR nextToken = nullptr;
    wchar_t delims[] = L";";

    WCHAR pathToAddFull[MAX_PATH];
    _wfullpath(pathToAddFull, pathToAdd, MAX_PATH);
    trimTrailingChar(pathToAddFull, L'\\');

    LPWSTR token = nullptr;
    WCHAR tokenFull[MAX_PATH];
    WCHAR pathCopy[_MAX_ENV];
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
    WCHAR newPath[_MAX_ENV];
    newPath[0] = '\0';
    wcscat_s(newPath, _MAX_ENV, pathToAddFull);
    wcscat_s(newPath, _MAX_ENV, L";");
    wcscat_s(newPath, _MAX_ENV, path);
    SetEnvironmentVariable(L"PATH", newPath);
    //wprintf(L"%s\n"), newPath);
}

BOOL UpdateEnv()
{
    HANDLE mapFile = nullptr;
    WinstonEnvUpdate* env = nullptr;
    mapFile = OpenFileMapping(FILE_MAP_READ, FALSE, SHARED_MEM_NAME);

    if (mapFile == nullptr)
    {
        OutputDebugString(L"Error opening file map");
        return FALSE;
    }

    env = static_cast<WinstonEnvUpdate*>(MapViewOfFile(mapFile, FILE_MAP_READ, 0, 0, sizeof(WinstonEnvUpdate)));

    if (env == nullptr)
    {
        OutputDebugString(L"Error opening shared memory");
        return FALSE;
    }

    if(wcscmp(env->operation, L"prepend") == 0)
    {
        PrependPath(env->path);
    }
    else
    {
        OutputDebugString(L"Unrecognized operation");
    }

    if (env)
    {
        UnmapViewOfFile(env);
    }
    if (mapFile)
    {
        CloseHandle(mapFile);
    }
    return TRUE;
}

BOOL APIENTRY DllMain(HMODULE hModule,
                      DWORD ul_reason_for_call,
                      LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    {
        TCHAR buf[MAX_PATH] = {0};
        _stprintf_s(buf, L"Attached process: %d", GetCurrentProcessId());
        OutputDebugString(buf);
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