namespace midi;

public class MidiFileInfo
{

    public string FileName { get; set; }

    public string FilePath { get; set; }

    public int TrackCount { get; set; }

    public int NoteCount { get; set; }

    public List<MidiNote> Notes { get; set; }

    public long TotalMicroseconds { get; set; }


    public string TimeString
    {
        get
        {
            var s = TotalMicroseconds / 1000000;
            return $"{s / 60:D2}:{s % 60:D2}";
        }
    }

}
