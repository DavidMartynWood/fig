﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fig.Client.Attributes;
using Fig.Client.Enums;
using Fig.Client.Exceptions;
using Fig.Client.ExtensionMethods;
using Fig.Client.SettingVerification;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Newtonsoft.Json;

namespace Fig.Client
{
    public abstract class SettingsBase
    {
        private readonly ISettingDefinitionFactory _settingDefinitionFactory;
        private readonly ISettingVerificationDecompiler _settingVerificationDecompiler;

        protected SettingsBase()
            : this(new SettingDefinitionFactory(), new SettingVerificationDecompiler())
        {
        }

        protected SettingsBase(ISettingDefinitionFactory settingDefinitionFactory,
            ISettingVerificationDecompiler settingVerificationDecompiler)
        {
            _settingDefinitionFactory = settingDefinitionFactory;
            _settingVerificationDecompiler = settingVerificationDecompiler;
        }

        public abstract string ClientName { get; }

        public abstract string ClientSecret { get; }

        public void Initialize(IEnumerable<SettingDataContract> settings)
        {
            if (settings != null)
                SetPropertiesFromSettings(settings.ToList());
            else
                SetPropertiesFromDefaultValues();
        }

        public SettingsClientDefinitionDataContract CreateDataContract()
        {
            var dataContract = new SettingsClientDefinitionDataContract
            {
                Instance = null, // TODO
                Name = ClientName
            };

            var settings = GetSettingProperties()
                .Select(settingProperty => _settingDefinitionFactory.Create(settingProperty))
                .ToList();

            dataContract.Settings = settings;
            dataContract.DynamicVerifications = GetDynamicVerifications();
            dataContract.PluginVerifications = GetPluginVerifications();

            return dataContract;
        }

        private List<SettingDynamicVerificationDefinitionDataContract> GetDynamicVerifications()
        {
            var verificationAttributes = GetType()
                .GetCustomAttributes(typeof(VerificationAttribute), true)
                .Cast<VerificationAttribute>()
                .Where(v => v.VerificationType == VerificationType.Dynamic);

            var verifications = new List<SettingDynamicVerificationDefinitionDataContract>();
            foreach (var attribute in verificationAttributes)
            {
                var verificationClass = attribute.ClassDoingVerification;

                if (!verificationClass.GetInterfaces().Contains(typeof(ISettingVerification)))
                    throw new InvalidSettingVerificationException(
                        $"Verification class {verificationClass.Name} does not implement {nameof(ISettingVerification)}");

                var decompiledCode = _settingVerificationDecompiler.Decompile(verificationClass,
                    nameof(ISettingVerification.PerformVerification));

                verifications.Add(new SettingDynamicVerificationDefinitionDataContract
                {
                    Name = attribute.Name,
                    Description = attribute.Description,
                    TargetRuntime = attribute.TargetRuntime,
                    Code = decompiledCode,
                    SettingsVerified = attribute.SettingNames.ToList()
                });
            }

            return verifications;
        }

        private List<SettingPluginVerificationDefinitionDataContract> GetPluginVerifications()
        {
            var verificationAttributes = GetType()
                .GetCustomAttributes(typeof(VerificationAttribute), true)
                .Cast<VerificationAttribute>()
                .Where(v => v.VerificationType == VerificationType.Plugin);

            return verificationAttributes.Select(attribute => new SettingPluginVerificationDefinitionDataContract
            {
                Name = attribute.Name,
                Description = attribute.Description,
                PropertyArguments = attribute.SettingNames.ToList()
            }).ToList();
        }

        private IEnumerable<PropertyInfo> GetSettingProperties()
        {
            return GetType().GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(SettingAttribute)));
        }

        private void SetPropertiesFromDefaultValues()
        {
            foreach (var property in GetSettingProperties()) SetDefaultValue(property);
        }

        private void SetDefaultValue(PropertyInfo property)
        {
            if (property.GetCustomAttributes()
                    .FirstOrDefault(a => a.GetType() == typeof(SettingAttribute)) is SettingAttribute settingAttribute)
                if (settingAttribute.DefaultValue != null)
                    property.SetValue(this, settingAttribute.DefaultValue);
        }

        private void SetPropertiesFromSettings(List<SettingDataContract> settings)
        {
            foreach (var property in GetSettingProperties())
            {
                var definition = settings.FirstOrDefault(a => a.Name == property.Name);

                if (definition?.Value != null)
                {
                    if (property.PropertyType.IsEnum)
                        SetEnumValue(property, definition.Value);
                    else if (property.PropertyType.IsSupportedBaseType())
                        property.SetValue(this, definition.Value);
                    else if (property.PropertyType.IsSupportedDataGridType())
                        SetDataGridValue(property, definition.Value);
                    else
                        SetJsonValue(property, definition.Value);
                }
                else
                {
                    SetDefaultValue(property);
                }
            }
        }

        private void SetEnumValue(PropertyInfo property, string value)
        {
            var enumValue = Enum.Parse(property.PropertyType, value);
            property.SetValue(this, enumValue);
        }

        private void SetDataGridValue(PropertyInfo property, List<Dictionary<string, object>> dataGridRows)
        {
            var genericType = property.PropertyType.GetGenericArguments().First();
            var list = (IList) Activator.CreateInstance(property.PropertyType);
            foreach (var dataGridRow in dataGridRows)
            {
                var listItem = Activator.CreateInstance(genericType);

                foreach (var column in dataGridRow)
                {
                    var prop = genericType.GetProperty(column.Key);
                    if (prop?.PropertyType == typeof(int) && column.Value is long value)
                        prop.SetValue(listItem, (int?) value);
                    else
                        prop?.SetValue(listItem, column.Value);
                }

                list.Add(listItem);
            }

            property.SetValue(this, list);
        }

        private void SetJsonValue(PropertyInfo property, string value)
        {
            var deserializedValue = JsonConvert.DeserializeObject(value, property.PropertyType);
            property.SetValue(this, deserializedValue);
        }
    }
}