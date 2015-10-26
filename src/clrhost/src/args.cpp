#include "args.h"

arguments_t::arguments_t() :
    trace(false),
    managed_application(_X("")),
    clr_path(_X("")),
    app_argc(0),
    app_argv(nullptr)
{
}

void display_help()
{
    xerr <<
        _X("Usage: clrhost [OPTIONS] assembly [ARGUMENTS]\n")
        _X("Execute the specified managed assembly with the passed in arguments\n\n")
        _X("Options:\n")
        _X("-c, --clr-path <PATH>   path to the CoreCLR files\n")
        _X("-t, --trace             enable tracing\n");
}

bool is_arg(const pal::char_t* arg, const pal::char_t* shortOption, const pal::char_t* longOption)
{
    return pal::strcmp(arg, shortOption) == 0 ||
        pal::strcmp(arg, longOption) == 0;
}

std::pair<bool, arguments_t> parse_arguments(const int argc, const pal::char_t* argv[])
{
    arguments_t args;

    bool seen_app_path = false;

    int i = 1;
    for (; i < argc; i++)
    {
        // Check for an option
        if (argv[i][0] == '-')
        {
            // It's an option, read it
            if (is_arg(argv[i], _X("-t"), _X("--trace")))
            {
                args.trace = true;
            }
            else if (is_arg(argv[i], _X("-c"), _X("--clr-path")))
            {
                i++;
                auto resolved_clr_path = pal::realpath(pal::string_t(argv[i]));
                if (!resolved_clr_path.first)
                {
                    xerr << _X("Cannot locate CLR files: ") << argv[i] << std::endl;
                    display_help();
                    return std::make_pair(false, args);
                }
                args.clr_path = resolved_clr_path.second;
            }
            else
            {
                // Unknown argument
                xerr << _X("Unknown option: ") << argv[i] << std::endl;
                display_help();
                return std::make_pair(false, args);
            }
        }
        else
        {
            // It's not an option, it must be the app path
            seen_app_path = true;
            auto resolved_app_path = pal::realpath(pal::string_t(argv[i]));
            if (!resolved_app_path.first)
            {
                xerr << _X("Cannot locate managed application: ") << argv[i] << std::endl;
                display_help();
                return std::make_pair(false, args);
            }
            args.managed_application = resolved_app_path.second;
            break;
        }
    }

    if (!seen_app_path)
    {
        xerr << _X("Please specify the application to run") << std::endl;
        display_help();
        return std::make_pair(false, args);
    }

    // The remaining arguments are for the managed application
    args.app_argv = &argv[i];
    args.app_argc = argc - (i + 1);

    return std::make_pair(true, args);
}
