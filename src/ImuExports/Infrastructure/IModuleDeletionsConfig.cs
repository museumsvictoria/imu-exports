using System;
using System.Collections.Generic;
using IMu;

namespace ImuExports.Infrastructure
{
    public interface IModuleDeletionsConfig
    {
        string ModuleName { get; }

        string[] Columns { get; }

        Terms Terms { get; }

        Func<Map, IEnumerable<string>> SelectFunc { get; }
    }
}