#ifndef PAL_H
#define PAL_H

#include <string>
#include <vector>
#include <fstream>
#include <iostream>
#include <cstring>
#include <cstdarg>
#include <tuple>

#if defined(_WIN32)
#include <Windows.h>

#define xerr std::wcerr
#define xout std::wcout
#define DIR_SEPARATOR L"\\"
#define PATH_SEPARATOR L";"
#define PATH_MAX MAX_PATH
#define _X(s) L ## s

namespace pal
{
    typedef wchar_t char_t;
    typedef std::wstring string_t;
    typedef std::ifstream ifstream_t;

    class coreclr
    {
    private:
        HMODULE m_dlhandle;
        bool m_bound;

        // Prototype of the coreclr_initialize function from the libcoreclr.so
        HRESULT (*coreclr_initialize)(
                    const char* exePath,
                    const char* appDomainFriendlyName,
                    int propertyCount,
                    const char** propertyKeys,
                    const char** propertyValues,
                    void** hostHandle,
                    unsigned int* domainId);

        // Prototype of the coreclr_shutdown function from the libcoreclr.so
        HRESULT (*coreclr_shutdown)(
                    void* hostHandle,
                    unsigned int domainId);

        // Prototype of the coreclr_execute_assembly function from the libcoreclr.so
        HRESULT (*coreclr_execute_assembly)(
                    void* hostHandle,
                    unsigned int domainId,
                    int argc,
                    const char** argv,
                    const char* managedAssemblyPath,
                    unsigned int* exitCode);

    public:
        coreclr(void* dlhandle);
        ~coreclr();

        inline bool bound()
        {
            return m_dlhandle != nullptr &&
                coreclr_initialize != nullptr &&
                coreclr_shutdown != nullptr &&
                coreclr_execute_assembly != nullptr;
        }

        int initialize(
                const char_t* exe_path,
                const char_t* app_domain_friendly_name,
                const char_t** property_keys,
                const char_t** property_values,
                int property_count,
                void** host_handle,
                unsigned int* domain_id);

        int shutdown(void* host_handle, unsigned int domain_id);

        int execute_assembly(
                void* host_handle,
                unsigned int domain_id,
                int argc,
                const char_t** argv,
                const char_t* managed_assembly_path,
                unsigned int* exit_code);
    };

    inline int strcmp(const char_t* str1, const char_t* str2) { return ::wcscmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::_wcsicmp(str1, str2); }
    inline size_t strlen(const char_t* str) { return ::wcslen(str); }
    inline void err_vprintf(const char_t* format, va_list vl) { ::vfwprintf(stderr, format, vl); ::fputws(_X("\r\n"), stderr); }

    pal::string_t to_palstring(const std::string& str);

    string_t dirname(const string_t& path);
    std::pair<bool, string_t> realpath(const string_t& path);
    string_t basename(const string_t& path);
    ifstream_t open_read(const string_t& path);
    bool file_exists(const string_t& path);
    std::vector<pal::string_t> readdir(const string_t& path);

    coreclr load_coreclr(const string_t& clr_path);
}

#else

#include <cstdlib>
#include <libgen.h>

#define xerr std::cerr
#define xout std::cout
#define DIR_SEPARATOR "/"
#define PATH_SEPARATOR ":"

#define SUCCEEDED(Status) ((Status) >= 0)

#if defined(__APPLE__)
#define LIBCORECLR_NAME "libcoreclr.dylib"
#else
#define LIBCORECLR_NAME "libcoreclr.so"
#endif

namespace pal
{
    typedef char char_t;
    typedef std::string string_t;
    typedef std::ifstream ifstream_t;

    class coreclr
    {
    private:
        void* m_dlhandle;
        bool m_bound;

        // Prototype of the coreclr_initialize function from the libcoreclr.so
        int (*coreclr_initialize)(
                    const char* exePath,
                    const char* appDomainFriendlyName,
                    int propertyCount,
                    const char** propertyKeys,
                    const char** propertyValues,
                    void** hostHandle,
                    unsigned int* domainId);

        // Prototype of the coreclr_shutdown function from the libcoreclr.so
        int (*coreclr_shutdown)(
                    void* hostHandle,
                    unsigned int domainId);

        // Prototype of the coreclr_execute_assembly function from the libcoreclr.so
        int (*coreclr_execute_assembly)(
                    void* hostHandle,
                    unsigned int domainId,
                    int argc,
                    const char** argv,
                    const char* managedAssemblyPath,
                    unsigned int* exitCode);

    public:
        coreclr(void* dlhandle);
        ~coreclr();

        inline bool bound()
        {
            return m_dlhandle != nullptr &&
                coreclr_initialize != nullptr &&
                coreclr_shutdown != nullptr &&
                coreclr_execute_assembly != nullptr;
        }

        int initialize(
                const char_t* exe_path,
                const char_t* app_domain_friendly_name,
                const char_t** property_keys,
                const char_t** property_values,
                int property_count,
                void** host_handle,
                unsigned int* domain_id);

        int shutdown(void* host_handle, unsigned int domain_id);

        int execute_assembly(
                void* host_handle,
                unsigned int domain_id,
                int argc,
                const char_t** argv,
                const char_t* managed_assembly_path,
                unsigned int* exit_code);
    };

    inline int strcmp(const char_t* str1, const char_t* str2) { return ::strcmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::strcasecmp(str1, str2); }
    inline size_t strlen(const char_t* str) { return ::strlen(str); }
    inline void err_vprintf(const char_t* format, va_list vl) { ::vfprintf(stderr, format, vl); ::fputc('\n', stderr); }
    inline pal::string_t to_palstring(const std::string& str) { return str;  }

    string_t dirname(const string_t& path);
    std::pair<bool, string_t> realpath(const string_t& path);
    string_t basename(const string_t& path);
    ifstream_t open_read(const string_t& path);
    bool file_exists(const string_t& path);
    std::vector<pal::string_t> readdir(const string_t& path);

    coreclr load_coreclr(const string_t& clr_path);
}

#define _X(s) s

#endif // _WIN32

#endif // PAL_H
