using System.Diagnostics;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Enums;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Utils;
using Fig.Api.Validators;
using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

public class StatusService : AuthenticatedService, IStatusService
{
    private readonly IClientStatusConverter _clientStatusConverter;
    private readonly IClientStatusRepository _clientStatusRepository;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMemoryLeakAnalyzer _memoryLeakAnalyzer;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ILogger<StatusService> _logger;
    private readonly IWebHookDisseminationService _webHookDisseminationService;
    private readonly IClientRunSessionRepository _clientRunSessionRepository;
    private string? _requesterHostname;
    private string? _requestIpAddress;

    public StatusService(
        IClientStatusRepository clientStatusRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        IClientStatusConverter clientStatusConverter,
        IConfigurationRepository configurationRepository,
        IMemoryLeakAnalyzer memoryLeakAnalyzer,
        ILogger<StatusService> logger,
        IWebHookDisseminationService webHookDisseminationService,
        IClientRunSessionRepository clientRunSessionRepository)
    {
        _clientStatusRepository = clientStatusRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _clientStatusConverter = clientStatusConverter;
        _configurationRepository = configurationRepository;
        _memoryLeakAnalyzer = memoryLeakAnalyzer;
        _logger = logger;
        _webHookDisseminationService = webHookDisseminationService;
        _clientRunSessionRepository = clientRunSessionRepository;
    }

    public async Task<StatusResponseDataContract> SyncStatus(
        string clientName,
        string? instance,
        string clientSecret,
        StatusRequestDataContract statusRequest)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var client = _clientStatusRepository.GetClient(clientName, instance);

        if (client is null && !string.IsNullOrEmpty(instance))
            client = _clientStatusRepository.GetClient(clientName);
        
        if (client is null)
            throw new KeyNotFoundException($"No existing registration for client '{clientName}'");

        var registrationStatus = RegistrationStatusValidator.GetStatus(client, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
            throw new UnauthorizedAccessException();

        await RemoveExpiredSessions(client);
        var configuration = _configurationRepository.GetConfiguration();
        
        var session = client.RunSessions.FirstOrDefault(a => a.RunSessionId == statusRequest.RunSessionId);
        
        if (session is not null)
        {
            if (session.HasConfigurationError != statusRequest.HasConfigurationError)
                await HandleConfigurationErrorStatusChanged(statusRequest, client);
            
            session.Update(statusRequest, _requesterHostname, _requestIpAddress, configuration);
        }
        else
        {
            _logger.LogInformation("Creating new run session for client {clientName} with id {runSessionId}. StartTime:{startTime}", clientName, statusRequest.RunSessionId, statusRequest.StartTime);
            session = new ClientRunSessionBusinessEntity
            {
                RunSessionId = statusRequest.RunSessionId,
                StartTimeUtc = statusRequest.StartTime,
                LiveReload = true,
                PollIntervalMs = statusRequest.PollIntervalMs,
                LastSettingLoadUtc = DateTime.UtcNow // Assume it loaded settings on startup.
            };
            session.Update(statusRequest, _requesterHostname, _requestIpAddress, configuration);
            client.RunSessions.Add(session);
            _eventLogRepository.Add(_eventLogFactory.NewSession(session, client));
            if (statusRequest.HasConfigurationError)
                await HandleConfigurationErrorStatusChanged(statusRequest, client);
            await _webHookDisseminationService.ClientConnected(session, client);
        }

        if (configuration.AnalyzeMemoryUsage)
        {
            var memoryAnalysis = _memoryLeakAnalyzer.AnalyzeMemoryUsage(session);
            if (memoryAnalysis is not null)
            {
                session.MemoryAnalysis = memoryAnalysis;
                if (memoryAnalysis.PossibleMemoryLeakDetected)
                {
                    await _webHookDisseminationService.MemoryLeakDetected(client, session);
                }
            }
        }
        
        _clientStatusRepository.UpdateClientStatus(client);

        var updateAvailable = session.LiveReload && client.LastSettingValueUpdate > statusRequest.LastSettingUpdate;
        var changedSettings = GetChangedSettingNames(updateAvailable,
            statusRequest.LastSettingUpdate,
            client.LastSettingValueUpdate ?? DateTime.MinValue,
            client.Name,
            client.Instance);

        var pollIntervalOverride = configuration.PollIntervalOverride;
        
        return new StatusResponseDataContract
        {
            SettingUpdateAvailable = updateAvailable,
            PollIntervalMs = pollIntervalOverride ?? session.PollIntervalMs,
            AllowOfflineSettings = configuration.AllowOfflineSettings,
            RestartRequested = session.RestartRequested,
            ChangedSettings = changedSettings,
        };
    }

    public void SetLiveReload(Guid runSessionId, bool liveReload)
    {
        var runSession = _clientRunSessionRepository.GetRunSession(runSessionId);
        if (runSession is null)
            throw new KeyNotFoundException($"No run session registration for run session id {runSessionId}");

        var originalValue = runSession.LiveReload;
        
        runSession.LiveReload = liveReload;
        _clientRunSessionRepository.UpdateRunSession(runSession);
        
        _eventLogRepository.Add(_eventLogFactory.LiveReloadChange(runSession, originalValue, AuthenticatedUser));
    }
    
    public void RequestRestart(Guid runSessionId)
    {
        var runSession = _clientRunSessionRepository.GetRunSession(runSessionId);
        if (runSession is null)
            throw new KeyNotFoundException($"No run session registration for run session id {runSessionId}");
        
        runSession.RestartRequested = true;
        _clientRunSessionRepository.UpdateRunSession(runSession);
        
        _eventLogRepository.Add(_eventLogFactory.RestartRequested(runSession, AuthenticatedUser));
    }
    
    public List<ClientStatusDataContract> GetAll()
    {
        var clients = _clientStatusRepository.GetAllClients(AuthenticatedUser);
        return clients.Select(a => _clientStatusConverter.Convert(a))
            .Where(a => a.RunSessions.Any())
            .ToList();
    }

    public void SetRequesterDetails(string? ipAddress, string? hostname)
    {
        _requestIpAddress = ipAddress;
        _requesterHostname = hostname;
    }

    public void MarkRestartRequired(string clientName, string? instance)
    {
        var client = _clientStatusRepository.GetClient(clientName, instance);
        if (client == null)
            throw new KeyNotFoundException($"No existing registration for client '{clientName}'");

        foreach (var runSession in client.RunSessions)
        {
            runSession.RestartRequiredToApplySettings = true;
        }
        
        _clientStatusRepository.UpdateClientStatus(client);
    }

    private List<string>? GetChangedSettingNames(bool updateAvailable, DateTime startTime, DateTime endTime, string clientName, string? instance)
    {
        if (!updateAvailable)
            return null;

        var start = DateTime.SpecifyKind(startTime.AddSeconds(-1), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(endTime.AddSeconds(1), DateTimeKind.Utc);
        var valueChangeLogs = _eventLogRepository.GetSettingChanges(start, end, clientName, instance);
        return valueChangeLogs
            .Where(a => a.SettingName is not null)
            .Select(a => a.SettingName!)
            .Distinct()
            .ToList();
    }

    private async Task RemoveExpiredSessions(ClientStatusBusinessEntity client)
    {
        foreach (var session in client.RunSessions.ToList())
        {
            _logger.LogTrace(
                $"{session.Id}. Last seen:{session.LastSeen}. Poll interval: {session.PollIntervalMs}");
            if (session.IsExpired())
            {
                _logger.LogInformation("Removing expired session {runSessionId} for client {clientName}", session.RunSessionId, client.Name);
                client.RunSessions.Remove(session);
                _eventLogRepository.Add(_eventLogFactory.ExpiredSession(session, client));
                await _webHookDisseminationService.ClientDisconnected(session, client);
            }
        }
    }
    
    private async Task HandleConfigurationErrorStatusChanged(StatusRequestDataContract statusRequest,
        ClientStatusBusinessEntity client)
    {
        _eventLogRepository.Add(_eventLogFactory.ConfigurationErrorStatusChanged(client, statusRequest));

        foreach (var configurationError in statusRequest.ConfigurationErrors)
            _eventLogRepository.Add(_eventLogFactory.ConfigurationError(client, configurationError));

        await _webHookDisseminationService.ConfigurationErrorStatusChanged(client, statusRequest);
    }
}