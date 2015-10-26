#include "pal.h"
#include "args.h"
#include "host.h"
#include "trace.h"
#include "tpafile.h"

pal::string_t get_tpafile_path(const pal::string_t& app_base, const pal::string_t& app_name)
{
    pal::char_t buf[PATH_MAX];

    size_t idx = 0;

#pragma warning(disable: 4996) // Possibly unsafe copy
    app_base.copy(buf, app_base.length());
#pragma warning(default: 4996)

    idx += app_base.length();

    buf[idx] = DIR_SEPARATOR[0];
    idx++;

#pragma warning(disable: 4996) // Possibly unsafe copy
    app_name.copy(&buf[idx], app_name.length());
#pragma warning(default: 4996)
    idx += app_name.length();

    buf[idx] = _X('.'); buf[idx + 1] = _X('t'); buf[idx + 2] = _X('p'); buf[idx + 3] = _X('a');
    buf[idx + 4] = _X('\0');
    return pal::string_t(buf);
}

#if defined(_WIN32)
int __cdecl wmain(const int argc, const pal::char_t* argv[])
#else
int main(const int argc, const pal::char_t* argv[])
#endif
{
    // Parse arguments
    auto args_result = parse_arguments(argc, argv);
    if (!args_result.first)
    {
        return 1;
    }
    auto args = args_result.second;

    // Enable tracing
    auto trace = trace_writer(args.trace);
    trace.write(_X("Tracing enabled"));
    trace.write(_X("Preparing to launch managed application: %s"), args.managed_application.c_str());

    auto app_base = pal::dirname(args.managed_application);
    auto app_name = pal::basename(args.managed_application);

    if (args.clr_path.empty())
    {
        // Use the directory containing the managed assembly
        args.clr_path = app_base;
    }
    trace.write(_X("Using CLR files from: %s"), args.clr_path.c_str());

    trace.write(_X("Preparing to launch: %s"), app_name.c_str());

    // Check for and load tpa file
    auto tpafile_path = get_tpafile_path(app_base, app_name);
    trace.write(_X("Checking for TPA File at: %s"), tpafile_path.c_str());
    auto tparesult = tpafile::load(tpafile_path);
    if (!tparesult.first)
    {
        xerr << "Unable to read TPA file" << std::endl;
        return 1;
    }
    return host::run(args, app_base, tparesult.second, trace);
}
