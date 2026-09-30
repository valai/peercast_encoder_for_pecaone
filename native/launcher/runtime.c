// SPDX-License-Identifier: GPL-3.0-or-later
// Minimal compiler helpers, implemented without an external C runtime.
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <intrin.h>
#include <string.h>

#pragma function(memcpy)
void* __cdecl memcpy(void* destination, const void* source, size_t count)
{
    volatile unsigned char* output = (volatile unsigned char*)destination;
    const unsigned char* input = (const unsigned char*)source;
    while (count != 0) {
        *output++ = *input++;
        --count;
    }
    return destination;
}

__declspec(noreturn) void __cdecl __report_rangecheckfailure(void)
{
    __fastfail(FAST_FAIL_RANGE_CHECK_FAILURE);
}
