#include "host.h"
#include "tpafile.h"

int host::run(arguments_t args, pal::string_t app_base, tpafile tpa, trace_writer trace)
{
    tpa.add_from(app_base);

    // Build TPA list
    std::string tpalist;
    tpa.write_tpa_list(tpalist);

    // Trace the TPA list
    trace.write(_X("Using TPA list:"));
    trace.write(tpalist.c_str());

    // Build CoreCLR properties
    const char *property_keys[] = {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "APP_PATHS",
        "APP_NI_PATHS",
        //"NATIVE_DLL_SEARCH_DIRECTORIES",
        "AppDomainCompatSwitch"
    };
    const char *property_values[] = {
        // TRUSTED_PLATFORM_ASSEMBLIES
        tpalist.c_str(),
        // APP_PATHS
        app_base.c_str(),
        // APP_NI_PATHS
        app_base.c_str(),
        //// NATIVE_DLL_SEARCH_DIRECTORIES
        //nativeDllSearchDirs.c_str(),
        // AppDomainCompatSwitch
        "UseLatestBehaviorWhenTFMNotSpecified"
    };

    auto result = pal::execute_assembly(
        "something",
        property_keys,
        property_values,
        sizeof(property_keys) / sizeof(property_keys[0]),
        args.managed_application,
        args.app_argc,
        args.app_argv);
}
