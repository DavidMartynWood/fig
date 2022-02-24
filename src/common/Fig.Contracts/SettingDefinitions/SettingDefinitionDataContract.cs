﻿using System;
using System.Collections.Generic;
using Fig.Contracts.JsonConversion;
using Newtonsoft.Json;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingDefinitionDataContract
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsSecret { get; set; }

        [JsonConverter(typeof(DynamicObjectConverter))]
        public dynamic? Value { get; set; }

        [JsonConverter(typeof(DynamicObjectConverter))]
        public dynamic? DefaultValue { get; set; }

        public Type ValueType { get; set; }

        public ValidationType ValidationType { get; set; } = ValidationType.None;

        public string? ValidationRegex { get; set; }

        public string? ValidationExplanation { get; set; }

        public List<string>? ValidValues { get; set; }

        public string? Group { get; set; }

        public int? DisplayOrder { get; set; }

        public bool Advanced { get; set; }

        public string? StringFormat { get; set; }

        public int? EditorLineCount { get; set; }

        public string? JsonSchema { get; set; }

        public DataGridDefinitionDataContract? DataGridDefinition { get; set; }
    }
}