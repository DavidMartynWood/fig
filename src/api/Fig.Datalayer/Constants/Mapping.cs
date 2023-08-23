﻿namespace Fig.Datalayer.Constants;

public static class Mapping
{
    public static int NVarCharMax = 10000; // Anything over 4000 will be mapped to nvarchar(max)

    public static string ApiStatusTable = "api_status";
    public static string RunSessionsTable = "run_sessions";
    public static string RunSessionMemoryUsageTable = "run_session_memory_usages";
    public static string SettingClientsTable = "setting_clients";
    public static string DeferredClientImportsTable = "deferred_client_imports";
    public static string EventLogsTable = "event_logs";
    public static string ConfigurationTable = "configuration";
    public static string LookupTablesTable = "lookup_tables";
    public static string SettingsTable = "settings";
    public static string SettingPluginVerificationsTable = "setting_plugin_verifications";
    public static string DynamicVerificationsTable = "setting_dynamic_verifications";
    public static string SettingValueHistoryTable = "setting_value_history";
    public static string UsersTable = "users";
    public static string VerificationResultHistoryTable = "verification_result_history";
    public static string WebHookClientTable = "web_hook_clients";
    public static string WebHooksTable = "web_hooks";
}