using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace SpotifyDownloaderGUI;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _cts;
    private readonly string _defaultBackend = Path.Combine(AppContext.BaseDirectory, "backend", "spotify-dl.exe");

    public MainWindow()
    {
        InitializeComponent();
        OutputBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Spotify Downloads");
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select output folder",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(OutputBox.Text) ? OutputBox.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            OutputBox.Text = dialog.SelectedPath;
    }

    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Host, "open.spotify.com", StringComparison.OrdinalIgnoreCase))
        {
            System.Windows.MessageBox.Show(this, "Enter a valid Spotify URL.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var backend = File.Exists(_defaultBackend) ? _defaultBackend : "spotify-dl.exe";
        var format = ((ComboBoxItem)FormatBox.SelectedItem).Content?.ToString()?.ToLowerInvariant() ?? "mp3";
        var quality = ((ComboBoxItem)QualityBox.SelectedItem).Content?.ToString()?.Replace(" kbps", "") ?? "320";
        var output = OutputBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(output))
        {
            System.Windows.MessageBox.Show(this, "Select an output folder.", "Output folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Directory.CreateDirectory(output);
        SetRunning(true);
        _cts = new CancellationTokenSource();
        Progress.Value = 0;
        StatusText.Text = "Starting…";
        LogText.Text = "Launching backend without opening a console window.";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = backend,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = AppContext.BaseDirectory
            };
            psi.ArgumentList.Add("--url");
            psi.ArgumentList.Add(url);
            psi.ArgumentList.Add("--format");
            psi.ArgumentList.Add(format);
            psi.ArgumentList.Add("--quality");
            psi.ArgumentList.Add(quality);
            psi.ArgumentList.Add("--output");
            psi.ArgumentList.Add(output);

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, args) => AppendLog(args.Data);
            process.ErrorDataReceived += (_, args) => AppendLog(args.Data);

            if (!process.Start())
                throw new InvalidOperationException("Backend could not be started.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            StatusText.Text = "Downloading…";
            await process.WaitForExitAsync(_cts.Token);

            if (process.ExitCode == 0)
            {
                Progress.Value = 100;
                StatusText.Text = "Completed";
            }
            else
            {
                StatusText.Text = $"Backend failed (exit {process.ExitCode})";
            }
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Cancelled";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error";
            LogText.Text = ex.Message;
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            SetRunning(false);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void SetRunning(bool running)
    {
        DownloadButton.IsEnabled = !running;
        CancelButton.IsEnabled = running;
        UrlBox.IsEnabled = !running;
        FormatBox.IsEnabled = !running;
        QualityBox.IsEnabled = !running;
        OutputBox.IsEnabled = !running;
    }

    private void AppendLog(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        Dispatcher.Invoke(() =>
        {
            LogText.Text = line;
            var marker = line.IndexOf('%');
            if (marker > 0)
            {
                var start = marker - 3;
                while (start >= 0 && char.IsDigit(line[start])) start--;
                var number = line[(start + 1)..marker];
                if (double.TryParse(number, out var pct))
                    Progress.Value = Math.Clamp(pct, 0, 100);
            }
        });
    }
}
