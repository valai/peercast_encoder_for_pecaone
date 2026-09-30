// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 PecaOne Connect contributors.
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#define PATH_CAPACITY 32768
static WCHAR target[PATH_CAPACITY];
static WCHAR command[PATH_CAPACITY];
static WCHAR working_directory[PATH_CAPACITY];
static STARTUPINFOW startup;
static PROCESS_INFORMATION child;

static void fail(LPCWSTR message, UINT code)
{
    MessageBoxW(NULL, message, L"ぺかわん コネクト", MB_OK | MB_ICONERROR);
    ExitProcess(code);
}

// No C runtime is used: the launcher imports only Windows system DLLs.
void WINAPI LauncherEntry(void)
{
    static const WCHAR suffix[] = L"app\\PecaOneRelay.exe";
    const DWORD suffix_length = (DWORD)(sizeof(suffix) / sizeof(WCHAR)) - 1;
    DWORD length = GetModuleFileNameW(NULL, target, PATH_CAPACITY);
    DWORD directory_length;
    DWORD attributes;
    DWORD i;

    if (length == 0 || length >= PATH_CAPACITY)
        fail(L"起動する場所を確認できませんでした。別のフォルダへ展開してお試しください。", 1);

    directory_length = length;
    while (directory_length > 0 && target[directory_length - 1] != L'\\' && target[directory_length - 1] != L'/')
        --directory_length;
    if (directory_length == 0 || directory_length + suffix_length > PATH_CAPACITY - 4)
        fail(L"展開先の場所が長すぎます。短い名前のフォルダへ展開してお試しください。", 1);

    for (i = 0; i <= suffix_length; ++i)
        target[directory_length + i] = suffix[i];
    length = directory_length + suffix_length;

    attributes = GetFileAttributesW(target);
    if (attributes == INVALID_FILE_ATTRIBUTES || (attributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
        fail(L"起動に必要なファイルが見つかりません。\nZIPを「すべて展開」し、「app」フォルダと一緒に置いたまま起動してください。", 2);

    command[0] = L'"';
    for (i = 0; i < length; ++i)
        command[i + 1] = target[i];
    command[length + 1] = L'"';
    command[length + 2] = L'\0';

    for (i = 0; i < directory_length + 3; ++i)
        working_directory[i] = target[i];
    working_directory[directory_length + 3] = L'\0';

    startup.cb = (DWORD)sizeof(startup);
    if (!CreateProcessW(target, command, NULL, NULL, FALSE, 0, NULL, working_directory, &startup, &child))
        fail(L"ぺかわん コネクトを起動できませんでした。\nZIPをもう一度展開してお試しください。", 3);

    CloseHandle(child.hThread);
    CloseHandle(child.hProcess);
    ExitProcess(0);
}
