using System;

namespace AshesOfVelsingrad.Utilities;

/// <summary>
/// Environment-backed configuration required to create Bugasura issues.
/// </summary>
public sealed class BugasuraConfiguration
{
    public const string ApiKeyEnvironmentName = "BUGASURA_API_KEY";
    public const string TeamIdEnvironmentName = "BUGASURA_TEAM_ID";
    public const string SprintIdEnvironmentName = "BUGASURA_SPRINT_ID";
    public const string EndpointEnvironmentName = "BUGASURA_ISSUES_ENDPOINT";

    private const string DefaultIssuesEndpoint = "https://api.bugasura.io/issues/add";

    private BugasuraConfiguration(string apiKey, int teamId, int sprintId, string issuesEndpoint)
    {
        ApiKey = apiKey;
        TeamId = teamId;
        SprintId = sprintId;
        IssuesEndpoint = issuesEndpoint;
    }

    public string ApiKey { get; }

    public int TeamId { get; }

    public int SprintId { get; }

    public string IssuesEndpoint { get; }

    public static bool TryLoadFromEnvironment(out BugasuraConfiguration? configuration, out string errorMessage)
    {
        configuration = null;

        string? apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentName);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            errorMessage = $"Missing environment variable '{ApiKeyEnvironmentName}'.";
            return false;
        }

        string? teamIdRaw = Environment.GetEnvironmentVariable(TeamIdEnvironmentName);
        if (!int.TryParse(teamIdRaw, out int teamId) || teamId <= 0)
        {
            errorMessage = $"Environment variable '{TeamIdEnvironmentName}' must be a positive integer.";
            return false;
        }

        string? sprintIdRaw = Environment.GetEnvironmentVariable(SprintIdEnvironmentName);
        if (!int.TryParse(sprintIdRaw, out int sprintId) || sprintId <= 0)
        {
            errorMessage = $"Environment variable '{SprintIdEnvironmentName}' must be a positive integer.";
            return false;
        }

        string endpoint = Environment.GetEnvironmentVariable(EndpointEnvironmentName) ?? DefaultIssuesEndpoint;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? endpointUri) || endpointUri.Scheme != Uri.UriSchemeHttps)
        {
            errorMessage = $"Environment variable '{EndpointEnvironmentName}' must be a valid HTTPS URL.";
            return false;
        }

        configuration = new BugasuraConfiguration(apiKey.Trim(), teamId, sprintId, endpoint);
        errorMessage = string.Empty;
        return true;
    }
}
