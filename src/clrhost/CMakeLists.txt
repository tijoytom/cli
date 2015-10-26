cmake_minimum_required (VERSION 2.6)
project (clrhost)

include_directories(inc)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    src/args.cpp
    src/host.cpp
    src/main.cpp
    src/tpafile.cpp

    inc/args.h
    inc/host.h
    inc/pal.h
    inc/tpafile.h
    inc/trace.h)

if(WIN32)
    list(APPEND SOURCES src/pal.windows.cpp)
else()
    list(APPEND SOURCES src/pal.posix.cpp)
endif()

add_executable(clrhost ${SOURCES})
