using Fig.Common.Timer;
using Fig.Contracts.Status;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class ApiVersionFacade : IApiVersionFacade
{
    private readonly IHttpService _httpService;
    private readonly IPeriodicTimer _timer;

    public ApiVersionFacade(IHttpService httpService, ITimerFactory timerFactory)
    {
        _httpService = httpService;

        _timer = timerFactory.Create(TimeSpan.FromSeconds(10));

        ApiAddress = httpService.BaseAddress;
        
        Task.Run(async () => await Start());
    }

    public event EventHandler? IsConnectedChanged;
    public bool IsConnected { get; private set; }
    
    public DateTime? LastConnected { get; private set; }
    
    public string ApiAddress { get; }
    
    public string? ApiVersion { get; private set; }

    private async Task Start()
    {
        await PingApi();
        
        var source = new CancellationTokenSource();
        while (await _timer.WaitForNextTickAsync(source.Token) && !source.Token.IsCancellationRequested)
            await PingApi();
    }

    private async Task PingApi()
    {
        try
        {
            var result = await _httpService.Get<ApiVersionDataContract>("apiversion");

            if (result == null)
                throw new Exception("No Connection to API");
            
            IsConnected = true;
            ApiVersion = result.ApiVersion;
            LastConnected = DateTime.UtcNow;
            IsConnectedChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            IsConnected = false;
            IsConnectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}