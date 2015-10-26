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

    // Bind CoreCLR
    auto coreclr = pal::load_coreclr(args.clr_path);
    if (!coreclr.bound())
    {
        trace.write(_X("error: failed to bind to coreclr"));
    }

    // Initialize CoreCLR
    void* host_handle;
    unsigned int domain_id;
    auto hr = coreclr.initialize(
            args.managed_application.c_str(),
            "clrhost",
            property_keys,
            property_values,
            sizeof(property_keys) / sizeof(property_keys[0]),
            &host_handle,
            &domain_id);
    if (!SUCCEEDED(hr))
    {
        trace.write(_X("error: failed to initialize CoreCLR, HRESULT: 0x%X"), hr);
        return 1;
    }

    // Execute the application
    unsigned int exit_code = 1;
    hr = coreclr.execute_assembly(
            host_handle,
            domain_id,
            args.app_argc,
            args.app_argv,
            args.managed_application.c_str(),
            &exit_code);
    if (!SUCCEEDED(hr))
    {
        trace.write(_X("error: failed to execute managed app, HRESULT: 0x%X"), hr);
        return 1;
    }

    // Shut down the CoreCLR
    hr = coreclr.shutdown(host_handle, domain_id);
    if (!SUCCEEDED(hr))
    {
        trace.write(_X("error: failed to shut down CoreCLR, HRESULT: 0x%X"), hr);
    }
    return exit_code;
}
