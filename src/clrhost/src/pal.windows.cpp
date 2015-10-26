#include "pal.h"

pal::string_t pal::to_palstring(const std::string& str)
{
    // Calculate the size needed
    auto length = mbstowcs(nullptr, str.c_str(), str.length());

    // Allocate a string
    wchar_t* buf = new wchar_t[length];
    auto res = mbstowcs(buf, str.c_str(), str.length());
    if (res == -1)
    {
        return pal::string_t();
    }
    auto copied = pal::string_t(buf);
    delete[] buf;
    return copied;
}

pal::string_t pal::basename(const pal::string_t& path)
{
	// Find the last dir separator
	auto path_sep = path.find_last_of(DIR_SEPARATOR[0]);
	if (path_sep == pal::string_t::npos)
	{
		return pal::string_t(path);
	}

	return path.substr(path_sep);
}

pal::string_t pal::dirname(const pal::string_t& path)
{
	// Find the last dir separator
	auto path_sep = path.find_last_of(DIR_SEPARATOR[0]);
	if (path_sep == pal::string_t::npos)
	{
		return pal::string_t(path);
	}

	return path.substr(0, path_sep);
}

std::pair<bool, pal::string_t> pal::realpath(const pal::string_t& path)
{
    pal::char_t buf[MAX_PATH];
	auto res = ::GetFullPathNameW(path.c_str(), path.length(), buf, nullptr);
	if (res == 0 || res > MAX_PATH)
	{
		xerr << "error resolving path: " << path << std::endl;
		return std::make_pair(false, path);
	}
	return std::make_pair(true, pal::string_t(buf));
}

bool pal::file_exists(const pal::string_t& path)
{
	WIN32_FIND_DATAW data;
	auto find_handle = ::FindFirstFileW(path.c_str(), &data);
	bool found = find_handle != nullptr;
	::FindClose(find_handle);
	return found;
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