#pragma once

#pragma pack( push )
#pragma pack( 4 )

struct WinstonEnvUpdate
{
	WCHAR variable[128];
	WCHAR value[16384];

	WinstonEnvUpdate()
	{
		ZeroMemory(variable, sizeof(variable));
		ZeroMemory(value, sizeof(value));
	}
};

TCHAR SHARED_MEM_NAME[] = _T("WinstonEnvUpdate");

#pragma pack( pop )