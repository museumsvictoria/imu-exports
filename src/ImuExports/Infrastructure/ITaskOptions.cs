using System;

namespace ImuExports.Infrastructure
{
    public interface ITaskOptions
    {
        void Initialize();

        Type TypeOfTask { get; }
    }
}