using System;
using AshesOfVelsingrad.Utilities;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Utilities;

[TestFixture]
public class BugasuraConfigurationTests
{
    private string? _previousApiKey;
    private string? _previousTeamId;
    private string? _previousSprintId;
    private string? _previousEndpoint;

    [SetUp]
    public void SetUp()
    {
        _previousApiKey = Environment.GetEnvironmentVariable(BugasuraConfiguration.ApiKeyEnvironmentName);
        _previousTeamId = Environment.GetEnvironmentVariable(BugasuraConfiguration.TeamIdEnvironmentName);
        _previousSprintId = Environment.GetEnvironmentVariable(BugasuraConfiguration.SprintIdEnvironmentName);
        _previousEndpoint = Environment.GetEnvironmentVariable(BugasuraConfiguration.EndpointEnvironmentName);

        ClearVariables();
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(BugasuraConfiguration.ApiKeyEnvironmentName, _previousApiKey);
        Environment.SetEnvironmentVariable(BugasuraConfiguration.TeamIdEnvironmentName, _previousTeamId);
        Environment.SetEnvironmentVariable(BugasuraConfiguration.SprintIdEnvironmentName, _previousSprintId);
        Environment.SetEnvironmentVariable(BugasuraConfiguration.EndpointEnvironmentName, _previousEndpoint);
    }

    [Test]
    public void TryLoadFromEnvironment_ReturnsFalse_WhenApiKeyIsMissing()
    {
        Environment.SetEnvironmentVariable(BugasuraConfiguration.TeamIdEnvironmentName, "1");
        Environment.SetEnvironmentVariable(BugasuraConfiguration.SprintIdEnvironmentName, "2");

        bool loaded = BugasuraConfiguration.TryLoadFromEnvironment(out BugasuraConfiguration? config, out string error);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.False);
            Assert.That(config, Is.Null);
            Assert.That(error, Does.Contain(BugasuraConfiguration.ApiKeyEnvironmentName));
        });
    }

    [Test]
    public void TryLoadFromEnvironment_ReturnsFalse_WhenTeamIdIsInvalid()
    {
        Environment.SetEnvironmentVariable(BugasuraConfiguration.ApiKeyEnvironmentName, "key");
        Environment.SetEnvironmentVariable(BugasuraConfiguration.TeamIdEnvironmentName, "abc");
        Environment.SetEnvironmentVariable(BugasuraConfiguration.SprintIdEnvironmentName, "2");

        bool loaded = BugasuraConfiguration.TryLoadFromEnvironment(out _, out string error);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.False);
            Assert.That(error, Does.Contain(BugasuraConfiguration.TeamIdEnvironmentName));
        });
    }

    [Test]
    public void TryLoadFromEnvironment_ReturnsFalse_WhenSprintIdIsInvalid()
    {
        Environment.SetEnvironmentVariable(BugasuraConfiguration.ApiKeyEnvironmentName, "key");
        Environment.SetEnvironmentVariable(BugasuraConfiguration.TeamIdEnvironmentName, "1");
        Environment.SetEnvironmentVariable(BugasuraConfiguration.SprintIdEnvironmentName, "0");

        bool loaded = BugasuraConfiguration.TryLoadFromEnvironment(out _, out string error);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.False);
            Assert.That(error, Does.Contain(BugasuraConfiguration.SprintIdEnvironmentName));
        });
    }

    [Test]
    public void TryLoadFromEnvironment_UsesDefaultEndpoint_WhenEndpointIsNotProvided()
    {
        Environment.SetEnvironmentVariable(BugasuraConfiguration.ApiKeyEnvironmentName, "key");
        Environment.SetEnvironmentVariable(BugasuraConfiguration.TeamIdEnvironmentName, "10");
        Environment.SetEnvironmentVariable(BugasuraConfiguration.SprintIdEnvironmentName, "22");

        bool loaded = BugasuraConfiguration.TryLoadFromEnvironment(out BugasuraConfiguration? config, out string error);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.True);
            Assert.That(error, Is.Empty);
            Assert.That(config, Is.Not.Null);
            Assert.That(config!.IssuesEndpoint, Is.EqualTo("https://api.bugasura.io/issues/add"));
        });
    }

    [Test]
    public void TryLoadFromEnvironment_UsesCustomEndpoint_WhenProvided()
    {
        Environment.SetEnvironmentVariable(BugasuraConfiguration.ApiKeyEnvironmentName, "key");
        Environment.SetEnvironmentVariable(BugasuraConfiguration.TeamIdEnvironmentName, "10");
        Environment.SetEnvironmentVariable(BugasuraConfiguration.SprintIdEnvironmentName, "22");
        Environment.SetEnvironmentVariable(BugasuraConfiguration.EndpointEnvironmentName, "https://api.bugasura.io/issues/add");

        bool loaded = BugasuraConfiguration.TryLoadFromEnvironment(out BugasuraConfiguration? config, out string error);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.True);
            Assert.That(error, Is.Empty);
            Assert.That(config, Is.Not.Null);
            Assert.That(config!.TeamId, Is.EqualTo(10));
            Assert.That(config.SprintId, Is.EqualTo(22));
            Assert.That(config.ApiKey, Is.EqualTo("key"));
            Assert.That(config.IssuesEndpoint, Is.EqualTo("https://api.bugasura.io/issues/add"));
        });
    }

    private static void ClearVariables()
    {
        Environment.SetEnvironmentVariable(BugasuraConfiguration.ApiKeyEnvironmentName, null);
        Environment.SetEnvironmentVariable(BugasuraConfiguration.TeamIdEnvironmentName, null);
        Environment.SetEnvironmentVariable(BugasuraConfiguration.SprintIdEnvironmentName, null);
        Environment.SetEnvironmentVariable(BugasuraConfiguration.EndpointEnvironmentName, null);
    }
}
