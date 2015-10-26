#include <set>

#include "tpafile.h"

std::tuple<bool, int, pal::string_t> read_field(pal::string_t line, int offset)
{
    // The first character should be a '"'
    if (line[offset] != '"')
    {
        return std::make_tuple(false, 0, pal::string_t());
    }
    offset++;

    // Set up destination buffer (it can't be bigger than the original line)
    pal::char_t buf[line.length()];
    auto buf_offset = 0;

    // Iterate through characters in the string
    for(; offset < line.length(); offset++)
    {
        // Is this a '\'?
        if (line[offset] == '\\')
        {
            // Skip this character and read the next character into the buffer
            offset++;
            buf[buf_offset] = line[offset];
        }
        // Is this a '"'?
        else if(line[offset] == '\"')
        {
            // Done! Advance to the pointer after the input
            offset++;
            break;
        }
        else
        {
            // Take the character
            buf[buf_offset] = line[offset];
        }
        buf_offset++;
    }
    buf[buf_offset] = '\0';
    return std::make_tuple(true, offset, pal::string_t(buf));
}

std::pair<bool, tpafile> tpafile::load(pal::string_t path)
{
    std::vector<tpaentry_t> entries;

    // Check if the file exists, if not, return an empty file
    if (!pal::file_exists(path))
    {
        return std::make_pair(true, tpafile(false, entries));
    }

    // Open the file
    auto file = pal::open_read(path);
    if (!file.good())
    {
        // Failed to open the file! open_read already wrote the error out
        return std::make_pair(false, tpafile(false, entries));
    }

    // Read lines from the file
    while(true)
    {
        std::string line;
        std::getline(file, line);
        if (file.eof())
        {
            break;
        }

        auto offset = 0;

        tpaentry_t entry;

        // Read a field
#define READ_FIELD(name, last) \
            {                                                                   \
                auto field_res = read_field(line, offset);                      \
                if (!std::get<0>(field_res))                                    \
                {                                                               \
                    xerr << "invalid TPA file" << std::endl;                    \
                    return std::make_pair(false, tpafile(false, entries));      \
                }                                                               \
                offset = std::get<1>(field_res);                                \
                entry.name = std::get<2>(field_res);                            \
                if (!last && line[offset] != ',')                               \
                {                                                               \
                    xerr << "missing fields in TPA line" << std::endl;          \
                    return std::make_pair(false, tpafile(false, entries));      \
                }                                                               \
                offset++;                                                       \
            }
        READ_FIELD(asset_type, false)
        READ_FIELD(library_name, false)
        READ_FIELD(library_version, false)
        READ_FIELD(relative_path, true)

        entries.push_back(entry);
    }

    return std::make_pair(true, tpafile(true, entries));
}

void tpafile::add_from(const pal::string_t& dir)
{
    const char * const tpa_extensions[] = {
        ".ni.dll",      // Probe for .ni.dll first so that it's preferred if ni and il coexist in the same dir
        ".dll",
        ".ni.exe",
        ".exe",
        };

    std::set<pal::string_t> added_assemblies;

    // Get directory entries
    auto files = pal::readdir(dir);
    for (auto ext : tpa_extensions)
    {
        auto len = pal::strlen(ext);
        for (auto file : files)
        {
            // Can't be a match if it's the same length as the extension :)
            if (file.length() > len)
            {
                // Extract the same amount of text from the end of file name
                auto file_ext = file.substr((file.length() - len) + 1, len);

                // Check if this file name matches
                if (pal::strcasecmp(ext, file_ext.c_str()) == 0)
                {
                    // Get the assembly name by stripping the extension
                    // and add it to the set so we can de-dupe
                    auto asm_name = file.substr(0, file.length() - len);

                    // TODO(anurse): Also check if already in TPA file
                    if (added_assemblies.find(asm_name) == added_assemblies.end())
                    {
                        added_assemblies.insert(asm_name);

                        tpaentry_t entry;
                        entry.asset_type = pal::string_t(_X("runtime"));
                        entry.library_name = pal::string_t(asm_name);
                        entry.library_version = pal::string_t("");

                        pal::string_t relpath(dir);
                        relpath.append(PATH_SEPARATOR);
                        relpath.append(file);
                        entry.relative_path = relpath;

                        m_entries.push_back(entry);
                    }
                }
            }
        }
    }
}

void tpafile::write_tpa_list(std::string& output)
{
    // TODO(anurse): De-dupe and resolve real paths instead of requiring absolute paths
    for (auto entry : m_entries)
    {
        output.append(entry.relative_path);
        output.append(NEWLINE);
    }
}
