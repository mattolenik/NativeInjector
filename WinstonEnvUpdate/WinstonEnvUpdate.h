#pragma once

#include "stdafx.h"

#pragma pack( push )
#pragma pack( 1 )

struct WinstonEnvUpdate
{
	WCHAR operation[24];
	WCHAR path[1000];

	WinstonEnvUpdate()
	{
		ZeroMemory(operation, sizeof(operation));
		ZeroMemory(path, sizeof(path));
	}
};

#pragma pack( pop )

TCHAR SHARED_MEM_NAME[] = L"WinstonEnvUpdate";