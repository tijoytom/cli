#include "pal.h"

#if defined(_WIN32)

#else

#include <dlfcn.h>
#include <dirent.h>
#include <sys/stat.h>

pal::string_t pal::basename(const pal::string_t& path)
{
    pal::char_t buf[path.length() + 1];
    path.copy(buf, path.length());
    buf[path.length()] = _X('\0');

    auto name = ::basename(buf);

    // Return the whole thing
    return pal::string_t(name);
}

pal::string_t pal::dirname(const pal::string_t& path)
{
    // Almost definitely not the most efficient way of doing this.
    pal::char_t buf[path.length() + 1];
    path.copy(buf, path.length());
    buf[path.length()] = _X('\0');
    auto dir = ::dirname(buf);
    return pal::string_t(dir);
}

std::pair<bool, pal::string_t> pal::realpath(const pal::string_t& path)
{
    pal::char_t buf[PATH_MAX];
    auto resolved = ::realpath(path.c_str(), buf);
    if (resolved == nullptr)
    {
        if (errno == ENOENT)
        {
            return std::make_pair(false, path);
        }
        perror("realpath()");
        return std::make_pair(false, path);
    }
    auto copied = pal::string_t(resolved);
    return std::make_pair(true, copied);
}

bool pal::file_exists(const pal::string_t& path)
{
    struct stat buffer;
    return (::stat(path.c_str(), &buffer) == 0);
}

pal::ifstream_t pal::open_read(const pal::string_t& path)
{
    return std::ifstream(path, std::ifstream::in);
}

std::vector<pal::string_t> pal::readdir(const pal::string_t& path)
{
    std::vector<pal::string_t> files;

    auto dir = opendir(path.c_str());
    if (dir != nullptr)
    {
        struct dirent* entry = nullptr;
        while((entry = readdir(dir)) != nullptr)
        {
            // We are interested in files only
            switch (entry->d_type)
            {
            case DT_REG:
                break;

            // Handle symlinks and file systems that do not support d_type
            case DT_LNK:
            case DT_UNKNOWN:
                {
                    std::string fullFilename;

                    fullFilename.append(path);
                    fullFilename.append(DIR_SEPARATOR);
                    fullFilename.append(entry->d_name);

                    struct stat sb;
                    if (stat(fullFilename.c_str(), &sb) == -1)
                    {
                        continue;
                    }

                    if (!S_ISREG(sb.st_mode))
                    {
                        continue;
                    }
                }
                break;

            default:
                continue;
            }

            files.push_back(pal::string_t(entry->d_name));
        }
    }

    return files;
}

// TODO(anurse): Move this elsewhere and use trace instead of xerr
//
//
// Prototype of the coreclr_initialize function from the libcoreclr.so
typedef int (*coreclr_initialize_fn)(
            const char* exePath,
            const char* appDomainFriendlyName,
            int propertyCount,
            const char** propertyKeys,
            const char** propertyValues,
            void** hostHandle,
            unsigned int* domainId);

// Prototype of the coreclr_shutdown function from the libcoreclr.so
typedef int (*coreclr_shutdown_fn)(
            void* hostHandle,
            unsigned int domainId);

// Prototype of the coreclr_execute_assembly function from the libcoreclr.so
typedef int (*coreclr_execute_assembly_fn)(
            void* hostHandle,
            unsigned int domainId,
            int argc,
            const char** argv,
            const char* managedAssemblyPath,
            unsigned int* exitCode);

std::pair<bool, int> pal::execute_assembly(
        const pal::string_t& clr_path,
        const pal::char_t* exe_path,
        const pal::char_t** property_keys,
        const pal::char_t** property_values,
        size_t property_count,
        const pal::string_t& managed_application,
        int app_argc,
        const pal::char_t** app_argv)
{
    pal::string_t libcoreclr_path(clr_path);
    libcoreclr_path.append(DIR_SEPARATOR);
    libcoreclr_path.append(LIBCORECLR_NAME);
    xerr << "Loading CoreCLR from: " << libcoreclr_path << std::endl;

    auto handle = dlopen(libcoreclr_path.c_str(), RTLD_LAZY);
    if (handle == nullptr)
    {
        xerr << "failed to load " LIBCORECLR_NAME " with error: " << dlerror() << std::endl;
        return std::make_pair(false, 0);
    }

    // Bind functions
    coreclr_initialize_fn coreclr_initialize = (coreclr_initialize_fn)dlsym(handle, "coreclr_initialize");
    if (coreclr_initialize == nullptr)
    {
        xerr << "failed to bind coreclr_initialize" << std::endl;
        return std::make_pair(false, 0);
    }
    coreclr_shutdown_fn coreclr_shutdown = (coreclr_shutdown_fn)dlsym(handle, "coreclr_initialize");
    if (coreclr_shutdown == nullptr)
    {
        xerr << "failed to bind coreclr_shutdown" << std::endl;
        return std::make_pair(false, 0);
    }
    coreclr_execute_assembly_fn coreclr_execute_assembly = (coreclr_execute_assembly_fn)dlsym(handle, "coreclr_execute_assembly");
    if (coreclr_execute_assembly == nullptr)
    {
        xerr << "failed to bind coreclr_execute_assembly" << std::endl;
        return std::make_pair(false, 0);
    }

    void* host_handle;
    unsigned int domain_id;

    auto st = coreclr_initialize(
            exe_path,
            "clrhost",
            property_count,
            property_keys,
            property_values,
            &host_handle,
            &domain_id);
    if (!SUCCEEDED(st))
    {
        xerr << "error initializing CoreCLR: 0x" << std::hex << st << std::endl;
        return std::make_pair(false, 0);
    }

    unsigned int exit_code;
    st = coreclr_execute_assembly(
            host_handle,
            domain_id,
            app_argc,
            app_argv,
            managed_application.c_str(),
            &exit_code);
    if (!SUCCEEDED(st))
    {
        xerr << "error executing application: 0x" << std::hex << st << std::endl;
        return std::make_pair(false, 0);
    }

    st = coreclr_shutdown(host_handle, domain_id);
    if (!SUCCEEDED(st))
    {
        xerr << "warning: error shutting down CoreCLR: 0x" << std::hex << st << std::endl;
    }

    if (!dlclose(handle))
    {
        xerr << "warning: failed to close libcoreclr" << std::endl;
    }
    return std::make_pair(true, exit_code);
}
#endif // _WIN32
