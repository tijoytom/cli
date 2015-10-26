#include "host.h"
#include "tpafile.h"

int host::run(arguments_t args, pal::string_t app_base, tpafile tpa, trace_writer trace)
{
    tpa.add_from(app_base);
    tpa.add_from(args.clr_path);

    // Build TPA list and search paths
    std::string tpalist;
    tpa.write_tpa_list(tpalist);

    std::string search_paths;
    tpa.write_native_paths(search_paths);

    // Build CoreCLR properties
    const char *property_keys[] = {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "APP_PATHS",
        "APP_NI_PATHS",
        "NATIVE_DLL_SEARCH_DIRECTORIES",
        "AppDomainCompatSwitch"
    };
    const char *property_values[] = {
        // TRUSTED_PLATFORM_ASSEMBLIES
        tpalist.c_str(),
        // APP_PATHS
        app_base.c_str(),
        // APP_NI_PATHS
        app_base.c_str(),
        // NATIVE_DLL_SEARCH_DIRECTORIES
        search_paths.c_str(),
        // AppDomainCompatSwitch
        "UseLatestBehaviorWhenTFMNotSpecified"
    };

    auto result = pal::execute_assembly(
            args.clr_path,
            "something",
            property_keys,
            property_values,
            sizeof(property_keys) / sizeof(property_keys[0]),
            args.managed_application,
            args.app_argc,
            args.app_argv);

    if (!result.first)
    {
        xerr << "error running application" << std::endl;
        return 1;
    }
    else
    {
        return result.second;
    }
}
