namespace midi;

public struct MidiNote
{

    public long AbsoluteMicrosecond { get; set; }

    public int Track { get; set; }

    public int Channel { get; set; }

    public int NoteNumber { get; set; }


}
