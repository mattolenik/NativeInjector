#pragma once

#include "stdafx.h"

#pragma pack( push )
#pragma pack( 1 )

struct EnvUpdate
{
	WCHAR operation[32];
	WCHAR path[_MAX_ENV];

	EnvUpdate()
	{
		ZeroMemory(operation, sizeof(operation));
		ZeroMemory(path, sizeof(path));
	}
};

#pragma pack( pop )

const TCHAR SHARED_MEM_NAME[] = L"EnvUpdate";
