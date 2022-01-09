using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Fig.Client;
using Fig.Contracts.Settings;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Api.Integration.Test;

public abstract class IntegrationTestBase
{
    protected HttpClient HttpClient;
    private WebApplicationFactory<Program> app;

    [OneTimeSetUp]
    public void FixtureSetup()
    {
        app = new WebApplicationFactory<Program>();
        HttpClient = app.CreateClient();
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        app.Dispose();
    }

    protected async Task RegisterSettings(SettingsBase settings)
    {
        var dataContract = settings.CreateDataContract();
        var json = JsonConvert.SerializeObject(dataContract);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        HttpClient.DefaultRequestHeaders.Add("clientSecret", settings.ClientSecret);
        var result = await HttpClient.PostAsync("/api/clients", data);
        
        Assert.That(result.IsSuccessStatusCode, Is.True, "Registration of settings should succeed.");
    }

    protected async Task<IEnumerable<SettingDataContract>> GetSettingsForClient(string clientName,
        string clientSecret, string? instance = null)
    {
        HttpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);
        var requestUri = $"/api/clients/{HttpUtility.UrlEncode(clientName)}/settings";
        if (instance != null)
        {
            requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";
        }
        
        var result = await HttpClient.GetStringAsync(requestUri);

        if (!string.IsNullOrEmpty(result))
        {
            return JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result);
        }

        return Array.Empty<SettingDataContract>();
    }
    
    protected async Task<IEnumerable<SettingsClientDataContract>> GetAllClients()
    {
        var result = await HttpClient.GetStringAsync("/api/clients");
        
        Assert.That(result, Is.Not.Null, "Get all clients should succeed.");

        return JsonConvert.DeserializeObject<IEnumerable<SettingsClientDataContract>>(result);
    }

    protected async Task SetSettings(string clientName, IEnumerable<SettingDataContract> settings, string? instance = null)
    {
        var json = JsonConvert.SerializeObject(settings);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        
        var requestUri = $"/api/clients/{HttpUtility.UrlEncode(clientName)}/settings";
        if (instance != null)
        {
            requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";
        }
        
        var result = await HttpClient.PutAsync(requestUri, data);
        
        Assert.That(result.IsSuccessStatusCode, Is.True, "Set of settings should succeed.");
    }

    protected async Task DeleteClient(string clientName, string? instance = null)
    {
        var requestUri = $"/api/clients/{HttpUtility.UrlEncode(clientName)}";
        if (instance != null)
        {
            requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";
        }
        
        var result = await HttpClient.DeleteAsync(requestUri);
        
        Assert.That(result.IsSuccessStatusCode, Is.True, "Delete of clients should succeed.");
    }

    protected async Task<OneStringSetting> RegisterOneStringSetting()
    {
        var settings = new OneStringSetting();
        await RegisterSettings(settings);
        return settings;
    }
    
    protected async Task<NoSettings> RegisterNoSettings()
    {
        var settings = new NoSettings();
        await RegisterSettings(settings);
        return settings;
    }
    
    protected async Task<ThreeSettings> RegisterThreeSettings()
    {
        var settings = new ThreeSettings();
        await RegisterSettings(settings);
        return settings;
    }
    
    protected async Task<AllSettingsAndTypes> RegisterAllSettingsAndTypes()
    {
        var settings = new AllSettingsAndTypes();
        await RegisterSettings(settings);
        return settings;
    }

    protected async Task DeleteAllClients()
    {
        var clients = await GetAllClients();
        foreach (var client in clients)
        {
            await DeleteClient(client.Name);
        }
    }
}