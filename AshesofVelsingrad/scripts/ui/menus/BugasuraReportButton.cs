using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AshesOfVelsingrad.Utilities;
using Godot;
using Environment = System.Environment;
using HttpClient = System.Net.Http.HttpClient;

public partial class BugasuraReportButton : Button
{
    private const string DefaultSummary = "Bug report from player";

    private AcceptDialog? _reportDialog;
    private LineEdit? _summaryInput;
    private TextEdit? _descriptionInput;
    private Label? _statusLabel;
    private bool _isSubmitting;

    public override void _Ready()
    {
        Pressed += OnReportButtonPressed;
    }

    private void OnReportButtonPressed()
    {
        EnsureReportDialog();
        ResetDialog();
        _reportDialog?.PopupCentered(new Vector2I(700, 500));
    }

    private void EnsureReportDialog()
    {
        if (_reportDialog != null)
            return;

        _reportDialog = new AcceptDialog
        {
            Title = "Report a bug",
            DialogText = "Describe the issue and send it to Bugasura.",
            OkButtonText = "Send",
            Exclusive = true,
        };

        var container = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };

        var summaryLabel = new Label
        {
            Text = "Summary",
        };
        _summaryInput = new LineEdit
        {
            PlaceholderText = "Short bug summary",
            MaxLength = 500,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };

        var descriptionLabel = new Label
        {
            Text = "Description",
        };
        _descriptionInput = new TextEdit
        {
            CustomMinimumSize = new Vector2(0, 180),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            WrapMode = TextEdit.LineWrappingMode.Boundary,
        };

        _statusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = Colors.LightGray,
            Visible = false,
        };

        container.AddChild(summaryLabel);
        container.AddChild(_summaryInput);
        container.AddChild(descriptionLabel);
        container.AddChild(_descriptionInput);
        container.AddChild(_statusLabel);

        _reportDialog.AddChild(container);
        _reportDialog.Confirmed += OnDialogConfirmed;
        AddChild(_reportDialog);
    }

    private void ResetDialog()
    {
        if (_summaryInput != null)
            _summaryInput.Text = DefaultSummary;

        if (_descriptionInput != null)
            _descriptionInput.Text = string.Empty;

        SetStatus(string.Empty, false);

        if (_reportDialog != null)
            _reportDialog.GetOkButton().Disabled = false;

        _isSubmitting = false;
    }

    private async void OnDialogConfirmed()
    {
        await SubmitReportAsync();
    }

    private async Task SubmitReportAsync()
    {
        if (_isSubmitting)
            return;

        if (_summaryInput == null || _descriptionInput == null)
        {
            SetStatus("Bug report UI is not initialized.", true);
            return;
        }

        if (!BugasuraConfiguration.TryLoadFromEnvironment(out BugasuraConfiguration? config, out string configError))
        {
            SetStatus(configError, true);
            return;
        }

        string summary = _summaryInput.Text.Trim();
        if (summary.Length < 5)
        {
            SetStatus("Summary must contain at least 5 characters.", true);
            return;
        }

        _isSubmitting = true;
        if (_reportDialog != null)
            _reportDialog.GetOkButton().Disabled = true;

        SetStatus("Sending report...", false);

        try
        {
            Vector2I resolution = DisplayServer.ScreenGetSize();

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {config!.ApiKey}");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

            using var form = new MultipartFormDataContent
            {
                { new StringContent(config.TeamId.ToString()), "team_id" },
                { new StringContent(config.SprintId.ToString()), "sprint_id" },
                { new StringContent(summary), "summary" },
                { new StringContent(_descriptionInput.Text.Trim()), "description" },
                { new StringContent("BUG"), "issue_type" },
                { new StringContent("MEDIUM"), "severity" },
                { new StringContent(RuntimeInformation.OSDescription), "os_name" },
                { new StringContent(Environment.OSVersion.VersionString), "os_version" },
                { new StringContent($"{resolution.X} x {resolution.Y}"), "resolution" },
            };

            using HttpResponseMessage response = await httpClient.PostAsync(config.IssuesEndpoint, form);
            string payload = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                SetStatus($"Bugasura request failed ({(int)response.StatusCode}).", true);
                GD.PrintErr($"Bugasura issue submission failed: {(int)response.StatusCode} - {payload}");
                return;
            }

            SetStatus("Bug report sent successfully.", false);
            _descriptionInput.Text = string.Empty;
        }
        catch (Exception exception)
        {
            SetStatus("Unable to send bug report. Please try again.", true);
            GD.PrintErr($"Bugasura issue submission exception: {exception}");
        }
        finally
        {
            _isSubmitting = false;
            if (_reportDialog != null)
                _reportDialog.GetOkButton().Disabled = false;
        }
    }

    private void SetStatus(string message, bool isError)
    {
        if (_statusLabel == null)
            return;

        _statusLabel.Text = message;
        _statusLabel.Visible = !string.IsNullOrWhiteSpace(message);
        _statusLabel.Modulate = isError ? Colors.IndianRed : Colors.LightGreen;
    }
}
