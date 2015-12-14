#define _CRT_SECURE_NO_WARNINGS
#include "stdafx.h"
#include <tchar.h>
#include <string>
#include "WinstonEnvUpdate.h"

BOOL UpdateEnv()
{
    HANDLE mapFile = nullptr;
    WinstonEnvUpdate* env = nullptr;
    mapFile = OpenFileMapping(FILE_MAP_READ, FALSE, SHARED_MEM_NAME);

    if (mapFile == nullptr)
    {
        OutputDebugString(TEXT("Error opening file map"));
        return FALSE;
    }

    env = static_cast<WinstonEnvUpdate*>(MapViewOfFile(mapFile, FILE_MAP_READ, 0, 0, sizeof(WinstonEnvUpdate)));

    if (env == nullptr)
    {
        OutputDebugString(TEXT("Error opening shared memory"));
        return FALSE;
    }

    if (wcslen(env->value))
    {
        SetEnvironmentVariableW(env->variable, env->value);
    }
    else
    {
        // Delete variable
        SetEnvironmentVariableW(env->variable, nullptr);
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
        _stprintf_s(buf, _T("Attached process: %d"), GetCurrentProcessId());
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