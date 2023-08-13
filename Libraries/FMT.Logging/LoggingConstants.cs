using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMT.Logging
{
    public static class LoggingConstants
    {
        public static string LoggingPath { get; } = Path.Combine(AppContext.BaseDirectory, "Logging");
    }
}
