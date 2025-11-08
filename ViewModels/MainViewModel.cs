using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Sharepoint_Video_Downloader.Models;

namespace Sharepoint_Video_Downloader.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private const string SettingsFile = "appsettings.txt";

        private string? _videoUrl;
        public string VideoUrl
        {
            get => _videoUrl ?? string.Empty;
            set
            {
                _videoUrl = value; NotifyPropertyChanged(nameof(VideoUrl));
                BuildFfmpgCmd();
            }
        }

        private string? _downloadPath;
        public string DownloadPath
        {
            get => _downloadPath ?? string.Empty;
            set { _downloadPath = value; NotifyPropertyChanged(nameof(DownloadPath)); SaveSettings(); BuildFfmpgCmd(); }
        }

        private string? _log;
        public string Log
        {
            get => _log ?? string.Empty;
            set { _log = value; NotifyPropertyChanged(nameof(Log)); }
        }

        public ObservableCollection<StreamInfo> VideoStreams { get; } = new();
        public ObservableCollection<StreamInfo> AudioStreams { get; } = new();

        private string? _ffmpgCmd;
        public string FfmpgCmd
        {
            get => _ffmpgCmd ?? string.Empty;
            set { _ffmpgCmd = value; NotifyPropertyChanged(nameof(FfmpgCmd)); }
        }

        private int _selectedVideoIndex = -1;
        public int SelectedVideoIndex
        {
            get => _selectedVideoIndex;
            set { _selectedVideoIndex = value; NotifyPropertyChanged(nameof(SelectedVideoIndex)); BuildFfmpgCmd(); }
        }

        private int _selectedAudioIndex = -1;
        public int SelectedAudioIndex
        {
            get => _selectedAudioIndex;
            set { _selectedAudioIndex = value; NotifyPropertyChanged(nameof(SelectedAudioIndex)); BuildFfmpgCmd(); }
        }

        public ICommand GetCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand ClearLogCommand { get; }

        public MainViewModel()
        {
            LoadSettings();

            GetCommand = new RelayCommand(async _ => await ExecuteGet());
            DownloadCommand = new RelayCommand(async _ => await ExecuteDownload(), _ => !string.IsNullOrWhiteSpace(FfmpgCmd));
            ClearLogCommand = new RelayCommand(_ => ExecuteClearLog());
        }

        private void NotifyPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private async Task ExecuteGet()
        {
            try
            {
                var sanitized = GetSanitizedUrl(VideoUrl);
                var cmd = string.Format("ffprobe -i \"{0}\" -show_streams", sanitized);
                AppendLog(cmd);

                // For testing use sample_output.txt in solution folder if present
                string output;
                var samplePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample_output.txt");
                if (File.Exists(samplePath))
                {
                    output = await File.ReadAllTextAsync(samplePath);
                }
                else
                {
                    output = await RunShellCommand(cmd);
                }

                AppendLog(output);
                ParseStreams(output);
            }
            catch (Exception ex)
            {
                AppendLog("E: " + ex.Message);
            }
        }

        private void ExecuteClearLog()
        {
            Log = string.Empty;
            NotifyPropertyChanged(nameof(Log));
        }

        private string GetSanitizedUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            var marker = "format=dash";
            var idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return url;
            var end = idx + marker.Length; // truncate right after 'format=dash'
            if (end > url.Length) end = url.Length;
            return url.Substring(0, end);
        }

        private void ParseStreams(string output)
        {
            VideoStreams.Clear();
            AudioStreams.Clear();

            var blocks = output.Split(new[] { "[STREAM]", "[/STREAM]" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var b in blocks)
            {
                var lines = b.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToArray();
                var dict = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var l in lines)
                {
                    var idx = l.IndexOf('=');
                    if (idx > 0)
                    {
                        var k = l.Substring(0, idx);
                        var v = l.Substring(idx + 1);
                        dict[k] = v;
                    }
                }

                if (dict.TryGetValue("codec_type", out var codecType) && dict.TryGetValue("index", out var index))
                {
                    dict.TryGetValue("TAG:id", out var tagId);
                    var display = tagId ?? (codecType + " " + index);
                    if (codecType == "video")
                    {
                        if (int.TryParse(index, out var idxVal))
                            VideoStreams.Add(new StreamInfo { Value = idxVal, DisplayText = display });
                    }
                    else if (codecType == "audio")
                    {
                        if (int.TryParse(index, out var idxVal))
                            AudioStreams.Add(new StreamInfo { Value = idxVal, DisplayText = display });
                    }
                }
            }

            if (VideoStreams.Count > 0)
                SelectedVideoIndex = VideoStreams[0].Value;
            if (AudioStreams.Count > 0)
                SelectedAudioIndex = AudioStreams[0].Value;
        }

        private void BuildFfmpgCmd()
        {
            if (string.IsNullOrWhiteSpace(VideoUrl) || SelectedVideoIndex < 0 || SelectedAudioIndex < 0) return;
            var sanitized = GetSanitizedUrl(VideoUrl);
            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var path = string.IsNullOrWhiteSpace(DownloadPath) ? "C:\\Downloads" : DownloadPath;
            // ensure path exists
            try { Directory.CreateDirectory(path); } catch { }
            var filename = Path.Combine(path, stamp + ".mp4");
            FfmpgCmd = string.Format("ffmpeg -i \"{0}\" -map 0:{1} -map 0:{2} -codec copy \"{3}\"", sanitized, SelectedVideoIndex, SelectedAudioIndex, filename);
            NotifyPropertyChanged(nameof(FfmpgCmd));
            AppendLog(FfmpgCmd);
        }

        private async Task<string> RunShellCommand(string command)
        {
            var tcs = new TaskCompletionSource<string>();
            try
            {
                var psi = new ProcessStartInfo("cmd", "/c " + command)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
                var output = new StringBuilder();
                p.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                p.ErrorDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                p.Exited += (_, __) => tcs.TrySetResult(output.ToString());
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                return "E: " + ex.Message;
            }
        }

        private async Task StartExternalShell(string command)
        {
            try
            {
                // Use cmd.exe /k to keep the window open after command finishes
                var psi = new ProcessStartInfo("cmd.exe", "/k " + command)
                {
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                AppendLog("E: " + ex.Message);
            }
            await Task.CompletedTask;
        }

        private async Task ExecuteDownload()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FfmpgCmd)) return;
                AppendLog(FfmpgCmd);
                // Start ffmpeg in a visible external shell so user can see progress
                await StartExternalShell(FfmpgCmd);
                AppendLog("Started external shell for download.");
            }
            catch (Exception ex)
            {
                AppendLog("E: " + ex.Message);
            }
        }

        private void SaveSettings()
        {
            try
            {
                var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFile);
                File.WriteAllText(file, DownloadPath ?? string.Empty);
            }
            catch { }
        }

        private void LoadSettings()
        {
            try
            {
                var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFile);
                if (File.Exists(file))
                {
                    DownloadPath = File.ReadAllText(file).Trim();
                    if (string.IsNullOrWhiteSpace(DownloadPath)) DownloadPath = "C:\\Downloads";
                }
                else
                {
                    DownloadPath = "C:\\Downloads";
                }
            }
            catch
            {
                DownloadPath = "C:\\Downloads";
            }
        }

        private void AppendLog(string text)
        {
            Log = (Log + "\n" + text).TrimStart('\n');
            NotifyPropertyChanged(nameof(Log));
        }
    }
}
