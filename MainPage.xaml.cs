using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Plugin.Maui.Audio;
using System.Diagnostics;

namespace Project2;

public partial class MainPage : ContentPage
{
    private List<Recording> recordings = new List<Recording>();
    private IAudioRecorder audioRecorder;
    private IAudioPlayer audioPlayer;
    private string currentRecordingPath;
    private DateTime recordingStartTime;
    private bool isRecording = false;
    private bool isPlaying = false;
    private Recording currentPlayingRecording;

    public MainPage()
    {
        InitializeComponent();

        try
        {
            // Не создаем player здесь - создадим при воспроизведении
            audioRecorder = AudioManager.Current.CreateRecorder();

            LoadRecordings();
        }
        catch (Exception ex)
        {
            DisplayAlert("Ошибка", $"Не удалось инициализировать диктофон: {ex.Message}", "OK");
        }
    }

    private async Task<bool> CheckPermissions()
    {
        try
        {
            var micStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (micStatus != PermissionStatus.Granted)
            {
                micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            }

            var storageStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (storageStatus != PermissionStatus.Granted)
            {
                storageStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
            }

            return micStatus == PermissionStatus.Granted &&
                   storageStatus == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Ошибка разрешений: {ex.Message}", "OK");
            return false;
        }
    }

    private async void OnRecordClicked(object sender, EventArgs e)
    {
        try
        {
            if (!await CheckPermissions())
            {
                await DisplayAlert("Ошибка", "Нет разрешений на запись аудио", "OK");
                return;
            }

            if (isPlaying)
            {
                if (audioPlayer != null)
                {
                    audioPlayer.Stop();
                    audioPlayer.Dispose();
                    audioPlayer = null;
                }
                isPlaying = false;
                playbackIndicator.IsVisible = false;
            }

            // Создаем папку для записей
            var recordingsFolder = Path.Combine(FileSystem.AppDataDirectory, "Recordings");
            if (!Directory.Exists(recordingsFolder))
            {
                Directory.CreateDirectory(recordingsFolder);
            }

            // Создаем имя файла
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"Запись_{timestamp}.wav";
            currentRecordingPath = Path.Combine(recordingsFolder, fileName);

            // Начинаем запись
            await audioRecorder.StartAsync(currentRecordingPath);

            isRecording = true;
            recordingStartTime = DateTime.Now;

            recordingIndicator.IsVisible = true;
            recordButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            stopButton.Text = "⏹️ Остановить запись";

            // Запускаем таймер
            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (isRecording)
                {
                    var elapsed = DateTime.Now - recordingStartTime;
                    timerLabel.Text = elapsed.ToString(@"mm\:ss");
                    return true;
                }
                return false;
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось начать запись: {ex.Message}", "OK");
        }
    }

    private async void OnStopClicked(object sender, EventArgs e)
    {
        try
        {
            if (isRecording)
            {
                await audioRecorder.StopAsync();
                isRecording = false;

                var duration = DateTime.Now - recordingStartTime;

                recordingIndicator.IsVisible = false;
                recordButton.IsEnabled = true;
                stopButton.IsEnabled = false;
                stopButton.Text = "⏹️ Стоп";

                // Сохраняем запись в список
                var recording = new Recording
                {
                    Name = $"Запись_{DateTime.Now:HH:mm}",
                    FilePath = currentRecordingPath,
                    Duration = duration,
                    CreatedDate = DateTime.Now
                };

                recordings.Add(recording);
                SaveRecordings();
                LoadRecordings();

                await DisplayAlert("Успех",
                    $"Запись сохранена!\nДлительность: {duration:mm\\:ss}", "OK");
            }
            else if (isPlaying)
            {
                if (audioPlayer != null)
                {
                    audioPlayer.Stop();
                    audioPlayer.Dispose();
                    audioPlayer = null;
                }
                isPlaying = false;
                playbackIndicator.IsVisible = false;
                stopButton.IsEnabled = false;
                stopButton.Text = "⏹️ Стоп";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
        }
    }

    private async void OnPlayClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is Recording recording)
            {
                if (isRecording)
                {
                    await DisplayAlert("Внимание", "Сначала остановите запись", "OK");
                    return;
                }

                if (isPlaying && audioPlayer != null)
                {
                    audioPlayer.Stop();
                    audioPlayer.Dispose();
                    audioPlayer = null;
                    isPlaying = false;
                }

                if (File.Exists(recording.FilePath))
                {
                    currentPlayingRecording = recording;

                    // Создаем player с файлом
                    audioPlayer = AudioManager.Current.CreatePlayer(recording.FilePath);

                    audioPlayer.PlaybackEnded += (s, args) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            isPlaying = false;
                            playbackIndicator.IsVisible = false;
                            stopButton.IsEnabled = false;
                            stopButton.Text = "⏹️ Стоп";
                        });
                    };

                    audioPlayer.Play();
                    isPlaying = true;

                    playbackIndicator.IsVisible = true;
                    stopButton.IsEnabled = true;
                    stopButton.Text = "⏹️ Остановить воспроизведение";

                    // Таймер воспроизведения
                    Device.StartTimer(TimeSpan.FromSeconds(1), () =>
                    {
                        if (isPlaying && audioPlayer != null)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                playbackTimerLabel.Text = audioPlayer.CurrentPosition.ToString(@"mm\:ss");
                            });
                            return true;
                        }
                        return false;
                    });
                }
                else
                {
                    await DisplayAlert("Ошибка", "Файл не найден", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось воспроизвести: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Recording recording)
        {
            var confirm = await DisplayAlert("Удаление",
                $"Удалить запись '{recording.Name}'?", "Удалить", "Отмена");

            if (confirm)
            {
                try
                {
                    if (File.Exists(recording.FilePath))
                    {
                        File.Delete(recording.FilePath);
                    }

                    recordings.Remove(recording);
                    SaveRecordings();
                    LoadRecordings();

                    await DisplayAlert("Успех", "Запись удалена", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось удалить: {ex.Message}", "OK");
                }
            }
        }
    }

    private void OnRefreshClicked(object sender, EventArgs e)
    {
        LoadRecordings();
    }

    private void LoadRecordings()
    {
        try
        {
            var recordingsFolder = Path.Combine(FileSystem.AppDataDirectory, "Recordings");

            if (!Directory.Exists(recordingsFolder))
            {
                recordings.Clear();
                recordingsList.ItemsSource = recordings;
                return;
            }

            var files = Directory.GetFiles(recordingsFolder, "*.wav")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            recordings.Clear();

            foreach (var file in files)
            {
                var recording = new Recording
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    FilePath = file,
                    CreatedDate = File.GetCreationTime(file),
                    Duration = TimeSpan.FromSeconds(30) // Временное значение
                };

                recordings.Add(recording);
            }

            recordingsList.ItemsSource = recordings;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка загрузки записей: {ex.Message}");
        }
    }

    private void SaveRecordings()
    {
        try
        {
            var filePath = Path.Combine(FileSystem.AppDataDirectory, "recordings.json");
            var json = System.Text.Json.JsonSerializer.Serialize(recordings);
            File.WriteAllText(filePath, json);
        }
        catch { }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (isRecording)
        {
            audioRecorder.StopAsync();
        }

        if (isPlaying && audioPlayer != null)
        {
            audioPlayer.Stop();
            audioPlayer.Dispose();
            audioPlayer = null;
        }
    }
}