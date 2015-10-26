#ifndef HOST_H
#define HOST_H

#include "args.h"
#include "trace.h"
#include "tpafile.h"

class host
{
public:
    static int run(arguments_t args, pal::string_t app_base, tpafile tpa, trace_writer trace);
};

#endif // HOST_H
