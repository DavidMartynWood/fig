﻿using Fig.Web.Models.Setting;

namespace Fig.Web.Events;

public class SettingEventModel : EventArgs
{
    public SettingEventModel(string name, SettingEventType eventType)
    {
        Name = name;
        EventType = eventType;
    }

    public SettingEventModel(string name, string message, SettingEventType eventType)
    {
        Name = name;
        EventType = eventType;
        Message = message;
    }

    public string Name { get; }

    public string? Message { get; set; }

    public SettingClientConfigurationModel? Client { get; set; }

    public SettingEventType EventType { get; }
}