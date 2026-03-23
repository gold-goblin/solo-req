using System.IO;
using Newtonsoft.Json;
using SoloReq.Models;

namespace SoloReq.Services;

public class HistoryService
{
    private readonly string _filePath;
    private CancellationTokenSource? _debounceCts;
    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };

    public HistoryService()
    {
        _filePath = PathResolver.Resolve("history.json");
    }

    public List<HistoryItem> Load()
    {
        if (!File.Exists(_filePath))
            return new List<HistoryItem>();

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<List<HistoryItem>>(json, _jsonSettings)
                   ?? new List<HistoryItem>();
        }
        catch
        {
            return new List<HistoryItem>();
        }
    }

    public void SaveDebounced(List<HistoryItem> items)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(1000, token);
                Save(items);
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private void Save(List<HistoryItem> items)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(items, _jsonSettings);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Silently ignore save errors
        }
    }
}
