// SPDX-License-Identifier: GPL-3.0-or-later
// Test helper, not distributed with the application.
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
static WCHAR report_path[32768];
static WCHAR buffer[32768];

static void write_text(HANDLE file, LPCWSTR text)
{
    DWORD written;
    DWORD bytes = (DWORD)lstrlenW(text) * (DWORD)sizeof(WCHAR);
    if (!WriteFile(file, text, bytes, &written, NULL) || written != bytes)
        ExitProcess(92);
}

void WINAPI TestChildEntry(void)
{
    HANDLE file;
    static const WCHAR bom[] = { 0xfeff, 0 };
    DWORD count = GetEnvironmentVariableW(L"PECAONE_LAUNCHER_TEST_REPORT", report_path, 32768);
    if (count == 0 || count >= 32768)
        ExitProcess(90);
    file = CreateFileW(report_path, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (file == INVALID_HANDLE_VALUE)
        ExitProcess(91);
    write_text(file, bom);
    if (!GetModuleFileNameW(NULL, buffer, 32768))
        ExitProcess(93);
    write_text(file, buffer);
    write_text(file, L"\r\n");
    if (!GetCurrentDirectoryW(32768, buffer))
        ExitProcess(94);
    write_text(file, buffer);
    write_text(file, L"\r\n");
    write_text(file, GetCommandLineW());
    write_text(file, L"\r\nOK\r\n");
    CloseHandle(file);
    ExitProcess(0);
}
