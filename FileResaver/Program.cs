using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

Console.Title = "File Resaver";
try
{
  _ = File.OpenWrite(Path.Combine(Path.GetTempPath(), "fileresaver_instance_lock"));
}
catch (IOException)
{
  // File Resaver is already running.
  return;
}
var config = JsonSerializer.Deserialize<ConfigJson>(File.ReadAllText("config.json"), new JsonSerializerOptions
{
  AllowTrailingCommas = true,
  PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  PropertyNameCaseInsensitive = true,
  ReadCommentHandling = JsonCommentHandling.Skip
});
Dictionary<string, bool> cache = new();
FileSystemWatcher fsw = new(config.Path)
{
  EnableRaisingEvents = true,
  IncludeSubdirectories = true,
  NotifyFilter = NotifyFilters.LastWrite // This thing is buggy, always has been.
};
foreach (var filter in config.Filter) fsw.Filters.Add(filter);
fsw.Changed += OnFileChange;
await Task.Delay(Timeout.Infinite);

async void OnFileChange(object sender, FileSystemEventArgs e)
{
  if (e.ChangeType != WatcherChangeTypes.Changed) return;
  var fileName = e.FullPath;
  if (!cache.TryAdd(fileName, true)) return; // Fix to suppress double-fire of this callback (due to LastWrite problem).
  Console.WriteLine(fileName);
  var contents = await File.ReadAllBytesAsync(fileName);
  _ = File.WriteAllBytesAsync(fileName, contents);
  _ = Task.Run(async () =>
  {
    await Task.Delay(TimeSpan.FromSeconds(1));
    cache.Remove(fileName);
  });
}

public sealed record ConfigJson(string Path, string[] Filter);
