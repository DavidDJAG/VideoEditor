using System.ComponentModel;
using System.Text;
using VideoEdit.Domain.Models;
using VideoEdit.Infrastructure.Services;

namespace VideoEdit.UI;

public partial class MainForm : Form
{
    private readonly ServiceBundle _services;
    private readonly BindingList<MediaJob> _jobs = [];
    private readonly BindingList<LogEntry> _history = [];
    private readonly IReadOnlyList<EncodingPreset> _presets = EncodingPreset.CreateDefaults();
    private AppSettings _settings;
    private MediaProbeResult? _lastProbeResult;
    private CancellationTokenSource? _probeValidationCts;

    public MainForm(ServiceBundle services)
    {
        _services = services;
        _settings = services.Settings;
        InitializeComponent();
        ConfigureBindings();
        LoadDefaults();
        WireEvents();
    }

    private void ConfigureBindings()
    {
        queueGrid.AutoGenerateColumns = false;
        queueGrid.DataSource = _jobs;
        historyGrid.AutoGenerateColumns = false;
        historyGrid.DataSource = _history;
    }

    private void LoadDefaults()
    {
        cmbOperation.DataSource = Enum.GetValues<MediaJobType>();
        cmbPreset.DataSource = _presets.ToList();
        cmbOperation.SelectedItem = MediaJobType.TrimVideo;
        chkOverwrite.Checked = true;
        chkAudioCopy.Checked = true;
        chkVideoCopy.Checked = true;
        chkShowCommand.Checked = _settings.ShowCommandPreview;
        txtFfmpegDir.Text = _settings.FfmpegDirectory ?? string.Empty;
        lblToolDiagnostic.Text = _services.ToolResolver.Resolve().DiagnosticSummary;
        UpdateCommandPreview();
    }

    private void WireEvents()
    {
        cmbOperation.SelectedIndexChanged += (_, _) => UpdateOperationUi();
        cmbPreset.SelectedIndexChanged += (_, _) => ApplySelectedPreset();
        txtInputPaths.TextChanged += (_, _) => UpdateCommandPreview();
        txtOutputPath.TextChanged += (_, _) => UpdateCommandPreview();
        txtStart.TextChanged += (_, _) => UpdateCommandPreview();
        txtEnd.TextChanged += (_, _) => UpdateCommandPreview();
        txtDuration.TextChanged += (_, _) => UpdateCommandPreview();
        txtVideoCodec.TextChanged += (_, _) => UpdateCommandPreview();
        txtAudioCodec.TextChanged += (_, _) => UpdateCommandPreview();
        txtCrf.TextChanged += (_, _) => UpdateCommandPreview();
        txtPresetValue.TextChanged += (_, _) => UpdateCommandPreview();
        txtAudioBitrate.TextChanged += (_, _) => UpdateCommandPreview();
        txtPixelFormat.TextChanged += (_, _) => UpdateCommandPreview();
        txtContainer.TextChanged += (_, _) => UpdateCommandPreview();
        chkAccurateSeek.CheckedChanged += (_, _) => UpdateCommandPreview();
        chkAudioCopy.CheckedChanged += (_, _) => UpdateCommandPreview();
        chkVideoCopy.CheckedChanged += (_, _) => UpdateCommandPreview();
        chkOverwrite.CheckedChanged += (_, _) => UpdateCommandPreview();

        btnBrowseInput.Click += (_, _) => BrowseInputFiles();
        btnBrowseOutput.Click += (_, _) => BrowseOutputFile();
        btnPreview.Click += (_, _) => UpdateCommandPreview(forceVisible: true);
        btnEnqueue.Click += async (_, _) => await EnqueueJobAsync();
        btnCancelJob.Click += async (_, _) => await _services.JobQueueService.CancelActiveJobAsync();
        btnBrowseProbeInput.Click += (_, _) => BrowseProbeInput();
        btnRunProbe.Click += async (_, _) => await RunProbeAsync();
        btnExportProbeText.Click += (_, _) => ExportProbe(asJson: false);
        btnExportProbeJson.Click += (_, _) => ExportProbe(asJson: true);
        btnBrowseFfmpegDir.Click += (_, _) => BrowseFfmpegFolder();
        btnSaveSettings.Click += async (_, _) => await SaveSettingsAsync();
        historyGrid.SelectionChanged += (_, _) => ShowSelectedHistory();

        _services.JobQueueService.JobUpdated += (_, job) => BeginInvoke(() => UpsertJob(job));
        _services.FfmpegService.ProgressChanged += (_, progress) => BeginInvoke(() => UpdateProgress(progress));
        Shown += async (_, _) => await LoadHistoryAsync();
    }

    private void UpdateOperationUi()
    {
        var operation = SelectedOperation;
        grpTrim.Enabled = operation == MediaJobType.TrimVideo;
        chkAudioCopy.Enabled = operation is MediaJobType.TrimVideo or MediaJobType.ExtractAudio or MediaJobType.Convert;
        chkVideoCopy.Enabled = operation is MediaJobType.TrimVideo or MediaJobType.ExtractVideo or MediaJobType.Convert;
        cmbPreset.Enabled = operation is MediaJobType.ExtractAudio or MediaJobType.Convert;
        if (operation == MediaJobType.Concat && string.IsNullOrWhiteSpace(txtContainer.Text))
        {
            txtContainer.Text = "mp4";
        }
        UpdateCommandPreview();
    }

    private MediaJobType SelectedOperation => cmbOperation.SelectedItem is MediaJobType value ? value : MediaJobType.TrimVideo;

    private void ApplySelectedPreset()
    {
        if (cmbPreset.SelectedItem is not EncodingPreset preset || !cmbPreset.Enabled)
        {
            return;
        }

        txtContainer.Text = preset.ContainerExtension;
        txtVideoCodec.Text = preset.VideoCodec ?? string.Empty;
        txtAudioCodec.Text = preset.AudioCodec ?? string.Empty;
        txtCrf.Text = preset.Crf ?? string.Empty;
        txtPresetValue.Text = preset.Preset ?? string.Empty;
        txtAudioBitrate.Text = preset.AudioBitrate ?? string.Empty;
        txtPixelFormat.Text = preset.PixelFormat ?? string.Empty;
        chkAudioCopy.Checked = preset.AudioCopy;
        chkVideoCopy.Checked = preset.VideoCopy;
    }

    private async Task EnqueueJobAsync()
    {
        try
        {
            var request = BuildRequest();
            var validation = _services.Validator.Validate(request);
            if (!validation.IsValid)
            {
                MessageBox.Show(string.Join(Environment.NewLine, validation.Errors), "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (request.JobType == MediaJobType.Concat)
            {
                var concatReport = await ValidateConcatCompatibilityAsync(request);
                if (!concatReport.IsCompatible)
                {
                    txtLiveLog.AppendText("[concat-check]" + Environment.NewLine + concatReport.Summary + Environment.NewLine + Environment.NewLine);
                    MessageBox.Show(
                        $"{concatReport.Summary}{Environment.NewLine}{Environment.NewLine}Sugerencia: recodifica primero los archivos para alinearlos y luego concatena.",
                        "Concatenacion no compatible",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    tabControl.SelectedTab = tabQueue;
                    return;
                }

                lblQueueStatus.Text = concatReport.Summary;
            }

            if (File.Exists(request.OutputPath))
            {
                var overwrite = MessageBox.Show("El archivo de salida ya existe. ¿Deseas sobrescribirlo?", "Confirmar sobrescritura", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (overwrite != DialogResult.Yes)
                {
                    return;
                }
            }

            var job = new MediaJob
            {
                Name = BuildJobName(request),
                Request = request,
                CommandPreview = _services.FfmpegService.BuildCommandPreview(request),
                LastMessage = "Pendiente"
            };

            _services.JobQueueService.Enqueue(job);
            UpsertJob(job);
            await LoadHistoryAsync();
            tabControl.SelectedTab = tabQueue;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo encolar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task<ConcatCompatibilityReport> ValidateConcatCompatibilityAsync(MediaJobRequest request)
    {
        _probeValidationCts?.Cancel();
        _probeValidationCts = new CancellationTokenSource();
        lblQueueStatus.Text = "Analizando compatibilidad de concatenacion con ffprobe...";
        txtLiveLog.AppendText("[concat-check] Iniciando analisis de compatibilidad..." + Environment.NewLine);
        var report = await _services.ConcatCompatibilityAnalyzer.AnalyzeAsync(request.InputPaths, _probeValidationCts.Token);
        txtLiveLog.AppendText("[concat-check] " + report.Summary + Environment.NewLine);
        return report;
    }

    private MediaJobRequest BuildRequest()
    {
        var inputPaths = txtInputPaths.Lines.Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        return new MediaJobRequest
        {
            JobType = SelectedOperation,
            InputPaths = inputPaths,
            OutputPath = txtOutputPath.Text.Trim(),
            TimeRange = BuildTimeRange(),
            AccurateSeek = chkAccurateSeek.Checked,
            OverwriteOutput = chkOverwrite.Checked,
            VideoCodec = txtVideoCodec.Text.TrimOrNull(),
            AudioCodec = txtAudioCodec.Text.TrimOrNull(),
            Crf = txtCrf.Text.TrimOrNull(),
            Preset = txtPresetValue.Text.TrimOrNull(),
            AudioBitrate = txtAudioBitrate.Text.TrimOrNull(),
            PixelFormat = txtPixelFormat.Text.TrimOrNull(),
            ContainerExtension = txtContainer.Text.TrimOrNull(),
            VideoCopy = chkVideoCopy.Checked,
            AudioCopy = chkAudioCopy.Checked,
            SelectedPresetName = cmbPreset.SelectedItem is EncodingPreset preset ? preset.Name : null
        };
    }

    private TimeRange? BuildTimeRange()
    {
        if (string.IsNullOrWhiteSpace(txtStart.Text) && string.IsNullOrWhiteSpace(txtEnd.Text) && string.IsNullOrWhiteSpace(txtDuration.Text))
        {
            return null;
        }

        return new TimeRange
        {
            Start = ParseTime(txtStart.Text),
            End = ParseTime(txtEnd.Text),
            Duration = ParseTime(txtDuration.Text)
        };
    }

    private static TimeSpan? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TimeSpan.TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Tiempo inválido: {value}. Usa formato hh:mm:ss o hh:mm:ss.fff");
    }

    private string BuildJobName(MediaJobRequest request) => request.JobType switch
    {
        MediaJobType.TrimVideo => "Recorte",
        MediaJobType.ExtractAudio => $"Extraer audio ({request.SelectedPresetName ?? request.AudioCodec ?? "custom"})",
        MediaJobType.ExtractVideo => "Extraer video",
        MediaJobType.Convert => $"Convertir ({request.SelectedPresetName ?? request.VideoCodec ?? "custom"})",
        MediaJobType.Concat => "Concatenar",
        _ => request.JobType.ToString()
    };

    private void UpdateCommandPreview(bool forceVisible = false)
    {
        try
        {
            var request = BuildRequest();
            txtCommandPreview.Text = request.InputPaths.Count == 0 ? string.Empty : _services.FfmpegService.BuildCommandPreview(request);
        }
        catch (Exception ex)
        {
            txtCommandPreview.Text = ex.Message;
        }

        txtCommandPreview.Visible = forceVisible || chkShowCommand.Checked;
    }

    private void BrowseInputFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Multiselect = SelectedOperation == MediaJobType.Concat,
            Filter = "Media files|*.mp4;*.mkv;*.mov;*.avi;*.mp3;*.wav;*.m4a|Todos|*.*",
            InitialDirectory = _settings.LastInputDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        txtInputPaths.Text = string.Join(Environment.NewLine, dialog.FileNames);
        _settings.LastInputDirectory = Path.GetDirectoryName(dialog.FileNames[0]);
    }

    private void BrowseOutputFile()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Todos|*.*",
            InitialDirectory = _settings.LastOutputDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            FileName = SuggestedOutputFileName()
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        txtOutputPath.Text = dialog.FileName;
        _settings.LastOutputDirectory = Path.GetDirectoryName(dialog.FileName);
    }

    private string SuggestedOutputFileName()
    {
        var firstInput = txtInputPaths.Lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        var baseName = string.IsNullOrWhiteSpace(firstInput) ? "salida" : Path.GetFileNameWithoutExtension(firstInput.Trim());
        var extension = txtContainer.Text.Trim();
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = SelectedOperation == MediaJobType.ExtractAudio ? "mp3" : "mp4";
        }

        return $"{baseName}_{SelectedOperation.ToString().ToLowerInvariant()}.{extension.TrimStart('.')}";
    }

    private async Task RunProbeAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtProbeInput.Text))
            {
                MessageBox.Show("Selecciona un archivo para inspeccionar.", "Inspección", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            txtProbeOutput.Text = "Ejecutando ffprobe...";
            _lastProbeResult = await _services.FfprobeService.ProbeAsync(txtProbeInput.Text.Trim(), CancellationToken.None);
            txtProbeOutput.Text = $"{_lastProbeResult.SummaryText}{Environment.NewLine}{Environment.NewLine}{_lastProbeResult.RawJson}";
            txtProbeCommand.Text = _services.FfprobeService.BuildCommandPreview(txtProbeInput.Text.Trim());
            tabControl.SelectedTab = tabProbe;
        }
        catch (Exception ex)
        {
            txtProbeOutput.Text = ex.Message;
        }
    }

    private void BrowseProbeInput()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Media files|*.mp4;*.mkv;*.mov;*.avi;*.mp3;*.wav;*.m4a|Todos|*.*",
            InitialDirectory = _settings.LastInputDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtProbeInput.Text = dialog.FileName;
            _settings.LastInputDirectory = Path.GetDirectoryName(dialog.FileName);
        }
    }

    private void ExportProbe(bool asJson)
    {
        if (_lastProbeResult is null)
        {
            MessageBox.Show("Todavía no hay resultados de inspección.", "Inspección", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = asJson ? "JSON|*.json" : "Texto|*.txt",
            FileName = asJson ? "ffprobe.json" : "ffprobe.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, asJson ? _lastProbeResult.RawJson : _lastProbeResult.SummaryText);
    }

    private void BrowseFfmpegFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            InitialDirectory = _settings.FfmpegDirectory ?? Environment.CurrentDirectory,
            Description = "Selecciona la carpeta bin que contiene ffmpeg.exe y ffprobe.exe"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtFfmpegDir.Text = dialog.SelectedPath;
        }
    }

    private async Task SaveSettingsAsync()
    {
        _settings.FfmpegDirectory = txtFfmpegDir.Text.TrimOrNull();
        _settings.ShowCommandPreview = chkShowCommand.Checked;
        await _services.SettingsStore.SaveAsync(_settings, CancellationToken.None);
        lblToolDiagnostic.Text = _services.ToolResolver.Resolve().DiagnosticSummary;
        UpdateCommandPreview();
        MessageBox.Show("Configuración guardada.", "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task LoadHistoryAsync()
    {
        var entries = await _services.LogStore.LoadRecentAsync(100, CancellationToken.None);
        _history.RaiseListChangedEvents = false;
        _history.Clear();
        foreach (var entry in entries)
        {
            _history.Add(entry);
        }
        _history.RaiseListChangedEvents = true;
        _history.ResetBindings();
        ShowSelectedHistory();
    }

    private void UpsertJob(MediaJob job)
    {
        var existing = _jobs.FirstOrDefault(item => item.Id == job.Id);
        if (existing is null)
        {
            _jobs.Add(job);
        }

        queueGrid.Refresh();
        lblQueueStatus.Text = $"{job.Name}: {job.Status} {job.LastMessage}";
        if (job.Status is MediaJobStatus.Completed or MediaJobStatus.Error or MediaJobStatus.Cancelled)
        {
            _ = LoadHistoryAsync();
        }
    }

    private void UpdateProgress(FfmpegProgressInfo progress)
    {
        if (progress.Percentage is { } value)
        {
            progressBar.Value = Math.Max(0, Math.Min(100, (int)value));
        }

        txtLiveLog.AppendText(progress.RawLine + Environment.NewLine);
        lblQueueStatus.Text = $"Procesado: {progress.ProcessedTime} | fps={progress.FramesPerSecond} | speed={progress.Speed}";
    }

    private void ShowSelectedHistory()
    {
        if (historyGrid.CurrentRow?.DataBoundItem is not LogEntry entry)
        {
            txtHistoryDetails.Clear();
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"{entry.CreatedAt:g} | {entry.JobName} | Exit={entry.ExitCode}");
        builder.AppendLine(entry.CommandText);
        builder.AppendLine();
        builder.AppendLine("STDERR:");
        builder.AppendLine(entry.StandardError);
        builder.AppendLine();
        builder.AppendLine("STDOUT:");
        builder.AppendLine(entry.StandardOutput);
        txtHistoryDetails.Text = builder.ToString();
    }
}

internal static class StringExtensions
{
    public static string? TrimOrNull(this string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
