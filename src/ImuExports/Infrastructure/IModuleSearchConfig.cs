using System;
using System.Collections.Generic;
using IMu;

namespace ImuExports.Infrastructure
{
    public interface IModuleSearchConfig
    {
        string ModuleName { get; }
        
        string ModuleSelectName { get; }

        string[] Columns { get; }

        Terms Terms { get; }

        Func<Map, IEnumerable<long>> IrnSelectFunc { get; }
    }
}