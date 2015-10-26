#ifndef ARGS_H
#define ARGS_H

#include "pal.h"

struct arguments_t
{
    bool trace;
    pal::string_t managed_application;
    pal::string_t clr_path;

    int app_argc;
    const pal::char_t** app_argv;

    arguments_t();
};

std::pair<bool, arguments_t> parse_arguments(const int argc, const pal::char_t* argv[]);

#endif // ARGS_H
