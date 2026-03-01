using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Services.Resolvers;

namespace EdsMediaArchiver.Services.Writers;

public interface IMvdhDateWriter
{
    Task<DateTimeOffset?> WriteDateToFileAsync(string filePath, string fileType, DateTimeOffset date);
}

[Obsolete("Not implemented")]
public class MvdhDateWriter(
    IMetadataWriter metadataWriter, 
    IFileTypeResolver fileTypeResolver) : IFileDateWriter
{
    public async Task<DateTimeOffset?> WriteDateToFileAsync(string filePath, string fileType, DateTimeOffset date)
    {
        throw new NotImplementedException("Will enable setting dates for .mp4 that weren't converted/compressed (as ffmpeg can set it during write). Unfinished.");

        // Calculate MP4 Timestamp (Seconds since Jan 1, 1904)
        DateTime mp4Epoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        uint secondsSinceEpoch = (uint)(date.UtcDateTime - mp4Epoch).TotalSeconds;

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
        using var reader = new BinaryReader(stream);
        using var writer = new BinaryWriter(stream);

        // Locate the 'mvhd' atom
        // We search for the 4-byte signature "mvhd"
        byte[] signature = "mvhd"u8.ToArray();
        long mvhdOffset = FindAtomOffset(stream, signature);

        if (mvhdOffset == -1) throw new Exception("Could not find mvhd atom.");

        // Navigate to the timestamps
        // mvhd structure: 
        // [4 bytes size] [4 bytes "mvhd"] [1 byte version] [3 bytes flags]
        stream.Position = mvhdOffset + 8;
        byte version = reader.ReadByte();
        stream.Position += 3; // Skip flags

        if (version == 0)
        {
            // Version 0: 32-bit timestamps
            // Order: Creation Time (4 bytes), Modification Time (4 bytes)
            byte[] timeBytes = BitConverter.GetBytes(secondsSinceEpoch);
            if (BitConverter.IsLittleEndian) Array.Reverse(timeBytes); // MP4 is Big Endian

            writer.Write(timeBytes); // Overwrite Creation Time
            writer.Write(timeBytes); // Overwrite Modification Time
        }
        else
        {
            // Version 1: 64-bit timestamps
            ulong seconds64 = (ulong)(date.UtcDateTime - mp4Epoch).TotalSeconds;
            byte[] timeBytes = BitConverter.GetBytes(seconds64);
            if (BitConverter.IsLittleEndian) Array.Reverse(timeBytes);

            writer.Write(timeBytes);
            writer.Write(timeBytes);
        }
    }

    private long FindAtomOffset(Stream stream, byte[] signature)
    {
        byte[] buffer = new byte[signature.Length];
        while (stream.Position < stream.Length - 4)
        {
            stream.ReadExactly(buffer, 0, buffer.Length);
            if (buffer.SequenceEqual(signature))
            {
                return stream.Position - signature.Length;
            }
            stream.Position -= (signature.Length - 1); // Slide the window
        }
        return -1;
    }
}
