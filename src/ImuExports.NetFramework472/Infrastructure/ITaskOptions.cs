using System;

namespace ImuExports.NetFramework472.Infrastructure
{
    public interface ITaskOptions
    {
        void Initialize();

        Type TypeOfTask { get; }
    }
}