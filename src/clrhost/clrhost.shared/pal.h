#ifndef PAL_H
#define PAL_H

#include <string>
#include <vector>
#include <fstream>
#include <iostream>
#include <cstring>
#include <cstdarg>

#if defined(_WIN32)
#define xerr std::cwerr
#define xout std::cwout
#define PATH_SEPARATOR L"\\"
#define NEWLINE L"\r\n"

namespace pal
{
    typedef wchar_t char_t;
    typedef std::wstring string_t;
    typedef std::ifwstream ifstream_t;

    inline int strcmp(const char_t* str1, const char_t* str2) { return ::wcscmp(str1, str2); }
    inline void err_vprintf(const char_t* format, va_list vl) { ::vfwprintf(stderr, format, vl); xerr << std::endl; }

    string_t dirname(const string_t& path);
    std::pair<bool, string_t> realpath(const string_t& path);
    string_t basename(const string_t& path);
    ifstream_t open_read(const string_t& path);
    bool file_exists(const string_t& path);
}

#define _X(s) L ## s

#else

#include <cstdlib>
#include <libgen.h>

#define xerr std::cerr
#define xout std::cout
#define PATH_SEPARATOR "/"
#define NEWLINE "\n"

namespace pal
{
    typedef char char_t;
    typedef std::string string_t;
    typedef std::ifstream ifstream_t;
    typedef void* dll_t;

    inline int strcmp(const char_t* str1, const char_t* str2) { return ::strcmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::strcasecmp(str1, str2); }
    inline int strlen(const char_t* str) { return ::strlen(str); }
    inline void err_vprintf(const char_t* format, va_list vl) { ::vfprintf(stderr, format, vl); ::fputc('\n', stderr); }

    string_t dirname(const string_t& path);
    std::pair<bool, string_t> realpath(const string_t& path);
    string_t basename(const string_t& path);
    ifstream_t open_read(const string_t& path);
    bool file_exists(const string_t& path);
    std::vector<pal::string_t> readdir(const string_t& path);

    std::pair<bool, int> execute_assembly(
        const char_t* exe_path,
        const char_t** property_keys,
        const char_t** property_values,
        size_t property_count,
        const string_t& managed_application,
        int app_argc,
        const string_t& app_argv);
}

#define _X(s) s

#endif // _WIN32

#endif // PAL_H
