using System.Collections.Generic;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Contracts;

public class SettingsDefinitionDataContractTests
{
    [Test]
    public void ShallSerializeAndDeserialize()
    {
        var settings = new List<SettingDefinitionDataContract>()
        {
            new("String Setting",
                "A setting",
                new StringSettingDataContract("Default"),
                false,
                typeof(string),
                null,
                ValidationType.Custom,
                @"\d",
                "Should be valid",
                group: "Group"),
            new("Int Setting",
                "An int setting",
                new IntSettingDataContract(2),
                false,
                typeof(int),
                null,
                ValidationType.Custom,
                @".\d",
                "Should be valid 2",
                group: "Group 2")
        };

        var dataContract = new SettingsClientDefinitionDataContract("Test", 
            null, 
            settings,
            new List<SettingPluginVerificationDefinitionDataContract>(),
            new List<SettingDynamicVerificationDefinitionDataContract>());

        var json = JsonConvert.SerializeObject(dataContract, JsonSettings.FigDefault);

        var serializedDataContract = JsonConvert.DeserializeObject<SettingsClientDefinitionDataContract>(json, JsonSettings.FigDefault);

        serializedDataContract.Should().BeEquivalentTo(dataContract);
    }
}