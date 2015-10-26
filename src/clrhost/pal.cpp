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

pal::coreclr pal::load_coreclr(const pal::string_t& clr_path)
{
    pal::string_t libcoreclr_path(clr_path);
    libcoreclr_path.append(DIR_SEPARATOR);
    libcoreclr_path.append(LIBCORECLR_NAME);
    xerr << "Loading CoreCLR from: " << libcoreclr_path << std::endl;

    auto handle = dlopen(libcoreclr_path.c_str(), RTLD_LAZY);
    if (handle == nullptr)
    {
        xerr << "failed to load " LIBCORECLR_NAME " with error: " << dlerror() << std::endl;
        return pal::coreclr(nullptr);
    }
    return pal::coreclr(handle);
}

pal::coreclr::coreclr(void* dlhandle) : m_dlhandle(dlhandle)
{
    if (dlhandle != nullptr)
    {
        coreclr_initialize = (decltype(coreclr_initialize))dlsym(dlhandle, "coreclr_initialize");
        coreclr_shutdown = (decltype(coreclr_shutdown))dlsym(dlhandle, "coreclr_initialize");
        coreclr_execute_assembly = (decltype(coreclr_execute_assembly))dlsym(dlhandle, "coreclr_execute_assembly");
    }
}

pal::coreclr::~coreclr()
{
    dlclose(m_dlhandle);
}

int pal::coreclr::initialize(
        const pal::char_t* exe_path,
        const pal::char_t* app_domain_friendly_name,
        const pal::char_t** property_keys,
        const pal::char_t** property_values,
        int property_count,
        void** host_handle,
        unsigned int* domain_id)
{
    return coreclr_initialize(
            exe_path,
            app_domain_friendly_name,
            property_count,
            property_keys,
            property_values,
            host_handle,
            domain_id);
}

int pal::coreclr::shutdown(void* host_handle, unsigned int domain_id)
{
    return coreclr_shutdown(host_handle, domain_id);
}

int pal::coreclr::execute_assembly(
        void* host_handle,
        unsigned int domain_id,
        int argc,
        const pal::char_t** argv,
        const pal::char_t* managed_assembly_path,
        unsigned int* exit_code)
{
    return coreclr_execute_assembly(
            host_handle,
            domain_id,
            argc,
            argv,
            managed_assembly_path,
            exit_code);
}
#endif // _WIN32
