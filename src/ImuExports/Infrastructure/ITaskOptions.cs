namespace ImuExports.Infrastructure
{
    public interface ITaskOptions
    {
        public Task Initialize(AppSettings appSettings)
        {
            return Task.CompletedTask;
        }

        Type TypeOfTask { get; }
        
        public Task CleanUp(AppSettings appSettings)
        {
            return Task.CompletedTask;
        }
    }
}