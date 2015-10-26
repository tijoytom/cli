#include "pal.h"

#if defined(_WIN32)

#else

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
                    fullFilename.append(PATH_SEPARATOR);
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

std::pair<bool, int> execute_assembly(
    const char_t* exe_path,
    const char_t** property_keys,
    const char_t** property_values,
    size_t property_count,
    const string_t& managed_application,
    int app_argc,
    const string_t& app_argv)
{
}
#endif // _WIN32
