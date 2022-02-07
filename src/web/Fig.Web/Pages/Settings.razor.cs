using Fig.Web.Events;
using Fig.Web.Models;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class Settings
{
    private bool _isSaveInProgress;
    private bool _isSaveDisabled => _selectedSettingClient?.IsValid != true && _selectedSettingClient?.IsDirty != true;
    private bool _isSaveAllInProgress;
    private bool _isSaveAllDisabled => _settingClients?.Any(a => a.IsDirty || a.IsValid) != true;
    private bool _isInstanceDisabled => _selectedSettingClient == null || _selectedSettingClient?.Instance != null;
    private bool _isDeleteInProgress;
    private bool _isDeleteDisabled => _selectedSettingClient == null;
    private string _instanceName;

    private List<SettingClientConfigurationModel> _settingClients { get; set; } = new List<SettingClientConfigurationModel>();
    private SettingClientConfigurationModel? _selectedSettingClient { get; set; }

    [Inject]
    private ISettingsDataService? _settingsDataService { get; set; }

    [Inject]
    private NotificationService _notificationService { get; set; }

    [Inject]
    private INotificationFactory _notificationFactory { get; set; }

    [Inject]
    private DialogService _dialogService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await _settingsDataService!.LoadAllClients();
        foreach (var client in _settingsDataService.SettingsClients.OrderBy(client => client.Name))
        {
            client.RegisterEventAction(SettingRequest);
            _settingClients.Add(client);
        }

        Console.WriteLine($"loaded {_settingClients?.Count} services");
        await base.OnInitializedAsync();
    }

    private async Task<object> SettingRequest(SettingEventModel settingEventArgs)
    {
        if (settingEventArgs.EventType == SettingEventType.SettingHistoryRequested)
        {
            // Simulate API call
            await Task.Delay(1000);
            // TODO: Currently mocked data - request the data for real. Notification if none found.
            return new List<SettingHistoryModel>()
            {
                new SettingHistoryModel
                {
                    DateTime = DateTime.Now - TimeSpan.FromHours(2),
                    Value = "Some old val",
                    User = "John"
                },
                new SettingHistoryModel
                {
                    DateTime = DateTime.Now - TimeSpan.FromHours(1),
                    Value = "previous value",
                    User = "Sue"
                }
            };
        }
        else if (settingEventArgs.EventType == SettingEventType.RunVerification)
        {
            await Task.Delay(1000);
            return new VerificationResultModel()
            {
                Succeeded = true,
                Message = "Ran the verification and it was successful",
                Logs = "Running verification....success"
            };
        }
        else
        {
            InvokeAsync(StateHasChanged);
        }

        return Task.CompletedTask;
    }

    private async Task OnSave()
    {
        try
        {
            _isSaveInProgress = true;
            var settingCount = await SaveClient(_selectedSettingClient);
            _selectedSettingClient?.MarkAsSaved();
            ShowNotification(_notificationFactory.Success("Save", $"Successfully saved {settingCount} setting(s)."));
        }
        catch (Exception ex)
        {
            ShowNotification(_notificationFactory.Failure("Save", $"Save Failed: {ex.Message}"));
            Console.WriteLine(ex);
        }
        finally
        {
            _isSaveInProgress = false;
        }
    }

    private async Task OnSaveAll()
    {
        _isSaveAllInProgress = true;

        try
        {
            List<int> successes = new List<int>();
            List<string> failures = new List<string>();
            foreach (var client in _settingClients)
            {
                try
                {
                    successes.Add(await SaveClient(client));
                    client.MarkAsSaved();
                }
                catch (Exception ex)
                {
                    failures.Add(ex.Message);
                }
            }

            if (failures.Any())
            {
                ShowNotification(_notificationFactory.Failure("Save All", $"Failed to save {failures.Count} clients. {successes.Sum()} settings saved."));
            }
            else if (successes.Any(a => a > 0))
            {
                ShowNotification(_notificationFactory.Success("Save All", $"Successfully saved {successes.Sum()} setting(s) from {successes.Count(a => a > 0)} client(s)."));
            }
        }
        finally
        {
            _isSaveAllInProgress = false;
        }
    }

    private async Task OnAddInstance()
    {
        if (_selectedSettingClient != null)
        {
            if (!await GetInstanceName(_selectedSettingClient.Name))
            {
                _instanceName = string.Empty;
                return;
            }

            var instance = _selectedSettingClient.CreateInstance(_instanceName);
            instance.RegisterEventAction(SettingRequest);
            var existingIndex = _settingClients.IndexOf(_selectedSettingClient);
            _settingClients.Insert(existingIndex + 1, instance);
            ShowNotification(_notificationFactory.Success("Instance", $"New instance for client '{_selectedSettingClient.Name}' created."));
            _instanceName = string.Empty;
            _selectedSettingClient = instance;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnDelete()
    {
        if (_selectedSettingClient != null && _settingsDataService != null)
        {
            var instancePart = $" (Instance: {_selectedSettingClient.Instance})";
            var confirmationName = $"{_selectedSettingClient.Name}{(_selectedSettingClient.Instance != null ? instancePart : String.Empty)}";
            if (!await GetDeleteConfirmation(confirmationName))
            {
                return;
            }

            try
            {
                var clientName = _selectedSettingClient.Name;
                var clientInstance = _selectedSettingClient.Instance;

                _isDeleteInProgress = true;
                await _settingsDataService.DeleteClient(_selectedSettingClient);
                _settingClients.Remove(_selectedSettingClient);
                _selectedSettingClient = null;
                var instanceNotification = clientInstance != null ? $" (instance '{clientInstance}')" : string.Empty;
                ShowNotification(_notificationFactory.Success("Delete", $"Client '{clientName}'{instanceNotification} deleted successfully."));
            }
            catch (Exception ex)
            {
                ShowNotification(_notificationFactory.Failure("Delete", $"Delete Failed: {ex.Message}"));
                Console.WriteLine(ex);
            }
            finally
            {
                _isDeleteInProgress = false;
            }
        }
    }

    private async Task<int> SaveClient(SettingClientConfigurationModel? client)
    {
        if (client != null && _settingsDataService != null)
            return await _settingsDataService.SaveClient(client);

        return 0;
    }

    private void ShowNotification(NotificationMessage message)
    {
        _notificationService.Notify(message);
    }
}