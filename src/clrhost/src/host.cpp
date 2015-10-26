#include "host.h"
#include "tpafile.h"

int host::run(arguments_t args, pal::string_t app_base, tpafile tpa, trace_writer trace)
{
    tpa.add_from(app_base);
    tpa.add_from(args.clr_path);

    // Build TPA list and search paths
    pal::string_t tpalist;
    tpa.write_tpa_list(tpalist);

    pal::string_t search_paths;
    tpa.write_native_paths(search_paths);

    // Build CoreCLR properties
    const pal::char_t *property_keys[] = {
        _X("TRUSTED_PLATFORM_ASSEMBLIES"),
        _X("APP_PATHS"),
        _X("APP_NI_PATHS"),
        _X("NATIVE_DLL_SEARCH_DIRECTORIES"),
        _X("AppDomainCompatSwitch")
    };
    const pal::char_t *property_values[] = {
        // TRUSTED_PLATFORM_ASSEMBLIES
        tpalist.c_str(),
        // APP_PATHS
        app_base.c_str(),
        // APP_NI_PATHS
        app_base.c_str(),
        // NATIVE_DLL_SEARCH_DIRECTORIES
        search_paths.c_str(),
        // AppDomainCompatSwitch
        _X("UseLatestBehaviorWhenTFMNotSpecified")
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
            _X("clrhost"),
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
