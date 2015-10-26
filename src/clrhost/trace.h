#ifndef TRACE_H
#define TRACE_H

#include "pal.h"

class trace_writer
{
public:
    trace_writer(bool on) : m_on(on) {}

    void write(const pal::char_t* format, ...)
    {
        if (m_on)
        {
            va_list vl;
            va_start(vl,format);
            pal::err_vprintf(format, vl);
            va_end(vl);
        }
    }

private:
    bool m_on;
};

#endif // TRACE_H
