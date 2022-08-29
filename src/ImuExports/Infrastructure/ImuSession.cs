using IMu;

namespace ImuExports.Infrastructure;

public class ImuSession : IDisposable
{
    private Session _session;
    private Module _module;
    private bool _disposed;

    public ImuSession(string moduleName, string host, int? port)
    {
        // Exit if host or port not set
        if (port == null || string.IsNullOrEmpty(host))
        {
            Log.Logger.Fatal("IMu host {Host} and port {Port} need to be set, exiting", host, port);
            Environment.Exit(Constants.ExitCodeError);
        }
        
        _session = new Session(host, port.Value);
        _session.Connect();

        _module = new Module(moduleName, _session);
    }

    public long FindKey(long irn)
    {
        return _module.FindKey(irn);
    }

    public long FindKeys(List<long> keys)
    {
        return _module.FindKeys(keys);
    }

    public long FindTerms(Terms terms)
    {
        return _module.FindTerms(terms);
    }

    public ModuleFetchResult Fetch(string flag, int offset, int count, string[] columns)
    {
        return _module.Fetch(flag, offset, count, columns);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                // Dispose managed resources.
                if (_session != null)
                {
                    _session.Disconnect();
                    _session = null;
                }

            // Dispose unmanaged managed resources.
            _disposed = true;
            _module = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}