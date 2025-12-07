using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace Project2;

public class AudioRecorder
{
    private bool isRecording = false;
    private string currentRecordingPath;
    private DateTime recordingStartTime;

    public event Action<TimeSpan> OnRecordingTimerUpdated;

    public async Task<bool> CheckPermissions()
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
            Debug.WriteLine($"Permission error: {ex.Message}");
            return false;
        }
    }

    public async Task<string> StartRecording()
    {
        if (!await CheckPermissions())
        {
            throw new Exception("Нет разрешений на запись аудио");
        }

       
        var recordingsFolder = Path.Combine(FileSystem.AppDataDirectory, "Recordings");
        if (!Directory.Exists(recordingsFolder))
        {
            Directory.CreateDirectory(recordingsFolder);
        }

        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"Recording_{timestamp}.wav";
        currentRecordingPath = Path.Combine(recordingsFolder, fileName);

        isRecording = true;
        recordingStartTime = DateTime.Now;

        
        _ = Task.Run(async () =>
        {
            while (isRecording)
            {
                var elapsed = DateTime.Now - recordingStartTime;
                OnRecordingTimerUpdated?.Invoke(elapsed);
                await Task.Delay(1000);
            }
        });

        
        File.WriteAllText(currentRecordingPath, "audio_data");

        return currentRecordingPath;
    }

    public async Task<TimeSpan> StopRecording()
    {
        if (!isRecording)
            return TimeSpan.Zero;

        isRecording = false;
        var duration = DateTime.Now - recordingStartTime;

       
       
        await Task.Delay(500);

        return duration;
    }

    public bool IsRecording => isRecording;

    public void PlayRecording(string filePath)
    {
        if (File.Exists(filePath))
        {
           
            Application.Current.MainPage.DisplayAlert(
                "Воспроизведение",
                $"Воспроизводится: {Path.GetFileName(filePath)}",
                "OK");
        }
    }

    public void DeleteRecording(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public List<string> GetAllRecordings()
    {
        var recordingsFolder = Path.Combine(FileSystem.AppDataDirectory, "Recordings");

        if (!Directory.Exists(recordingsFolder))
            return new List<string>();

        return Directory.GetFiles(recordingsFolder, "*.wav").ToList();
    }
}