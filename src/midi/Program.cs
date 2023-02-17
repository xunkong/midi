using midi;
using System.CommandLine;
using System.Text;
using System.Text.Json;

var rootCommand = new RootCommand();


var inputOption = new Option<string[]?>("--input", "Midi files or folder to check.");
inputOption.AddAlias("-i");

var fileListOption = new Option<string?>("--file-list", "Path to midi file list.");
fileListOption.AddAlias("-fl");

var outputOption = new Option<string>("--output", "Output result.");
outputOption.AddAlias("-o");

var relativePathOption = new Option<string?>("--relative-path", "Relative path shows in result.");
relativePathOption.AddAlias("-rp");


var checkCommand = new Command("check", "Check midi files, and output result.")
{
    inputOption, fileListOption, outputOption, relativePathOption,
};


checkCommand.SetHandler((inputs, filelist, output, relativePath) =>
{
    var list = new List<string>();
    if (inputs?.Any() ?? false)
    {
        list.AddRange(inputs);
    }
    if (File.Exists(filelist))
    {
        var text = File.ReadAllText(filelist);
        var l = JsonSerializer.Deserialize<List<string>>(text);
        if (l?.Any() ?? false)
        {
            list.AddRange(l);
        }
    }
    var files = GetMidiFiles(list);
    Console.WriteLine($"{files.Count} midi files need to check");
    if (files.Count == 0)
    {
        return;
    }
    Console.WriteLine("----------");
    CheckMidiFiles(files, output, relativePath);

}, inputOption, fileListOption, outputOption, relativePathOption);

rootCommand.AddCommand(checkCommand);

rootCommand.Invoke(args);






static List<string> GetMidiFiles(List<string> inputs)
{
    var list = new List<string>();

    foreach (var input in inputs)
    {
        Console.WriteLine($"input: {input}");
        var item = input.Replace("\"", "");
        if (File.Exists(item) && Path.GetExtension(item).ToLower() is ".mid" or ".midi")
        {
            list.Add(item);
        }
        if (Directory.Exists(item))
        {
            var files = Directory.GetFiles(item, "*.mid", SearchOption.AllDirectories);
            Console.WriteLine($"find {files.Length} midi files");
            list.AddRange(files);
        }
    }

    return list.Distinct().Order().ToList();
}






static void CheckMidiFiles(List<string> files, string output, string? relativePath = null)
{
    bool error = false;
    var results = new List<MidiCheckResult>(files.Count);

    foreach (var file in files)
    {
        try
        {
            Console.WriteLine($"check: {file}");
            var midi = MidiReader.ReadFile(file);
            var result = new MidiCheckResult
            {
                FileName = midi.FileName,
                FilePath = Path.GetRelativePath(relativePath ?? "./", midi.FilePath),
                Time = midi.TimeString,
                TrackCount = midi.TrackCount.ToString(),
                NoteCount = midi.NoteCount.ToString(),
            };
            result.HitRate_Windsong = (midi.Notes.Count(x => MidiNoteKeyboard.WindsongLyre.Contains(x.NoteNumber)) / (double)midi.Notes.Count).ToString("P1");
            result.HitRate_Vintage = (midi.Notes.Count(x => MidiNoteKeyboard.VintageLyre.Contains(x.NoteNumber)) / (double)midi.Notes.Count).ToString("P1");
            results.Add(result);
        }
        catch (Exception ex)
        {
            error = true;
            Console.Error.WriteLine(ex);
            var result = new MidiCheckResult
            {
                FileName = Path.GetFileNameWithoutExtension(file),
                FilePath = Path.GetRelativePath(relativePath ?? "./", Path.GetFullPath(file)),
                Error = ex.Message,
            };
            results.Add(result);
        }
    }

    var list = new List<MidiCheckResult>();

    if (string.IsNullOrWhiteSpace(relativePath))
    {
        list = results.OrderBy(x => x.FileName).ToList();
    }
    else
    {
        foreach (var group in results.GroupBy(x => Path.GetDirectoryName(x.FilePath)).OrderBy(x => x.Key))
        {
            list.Add(new MidiCheckResult { FileName = (group.Key + Path.DirectorySeparatorChar).Replace(@"\", @"\\") });
            list.AddRange(group.OrderBy(x => x.FileName));
        }
    }

    var sb = new StringBuilder();
    sb.AppendLine($"Thanks for your pull request, the following table shows result of {list.Count} MIDI files.");
    sb.AppendLine();
    sb.AppendLine($"| Folder / File | Time | Track Count | Note Count | Hit Rate (Windsong) | Hit Rate (Vintage) |{(error ? " Error |" : "")}");
    sb.AppendLine($"| --- | --- | --- | --- | --- | --- |{(error ? " --- |" : "")}");
    foreach (var item in list)
    {
        if (string.IsNullOrWhiteSpace(item.FilePath))
        {
            sb.AppendLine($"| **{item.FileName}** | {item.Time} | {item.TrackCount} | {item.NoteCount} | {item.HitRate_Windsong} | {item.HitRate_Vintage} |{(error ? $" {item.Error} |" : "")}");
        }
        else
        {
            sb.AppendLine($"| {item.FileName} | {item.Time} | {item.TrackCount} | {item.NoteCount} | {item.HitRate_Windsong} | {item.HitRate_Vintage} |{(error ? $" {item.Error} |" : "")}");
        }
    }

    var dir = Path.GetDirectoryName(output);
    if (!string.IsNullOrWhiteSpace(dir))
    {
        Directory.CreateDirectory(dir);
    }
    File.WriteAllText(output, sb.ToString());
}

