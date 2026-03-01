namespace EdsMediaArchiver.Definitions;

public static class MediaType
{
    // Image/Video Types
    public const string Apng = "APNG";
    public const string Gif = "GIF";
    public const string Webp = "WEBP";

    // Image Types
    public const string Avif = "AVIF";
    public const string Bmp = "BMP";
    public const string Heic = "HEIC";
    public const string Heif = "HEIF";
    public const string Jpeg = "JPEG";
    public const string Png = "PNG";
    public const string Tiff = "TIFF";

    // Video Types
    public const string Amv = "AMV";
    public const string Asf = "ASF";
    public const string Avi = "AVI";
    public const string Dv = "DV";
    public const string Dvr_ms = "DVR-MS";
    public const string F4V = "F4V";
    public const string Flv = "FLV";
    public const string Gxf = "GXF";
    public const string Lrv = "LRV";
    public const string M2Ts = "M2TS";
    public const string M4V = "M4V";
    public const string Mj2 = "MJ2";
    public const string Mjpeg = "MJPEG";
    public const string Mkv = "MKV";
    public const string Mod = "MOD";
    public const string Mov = "MOV";
    public const string Mp4 = "MP4";
    public const string Mpeg = "MPEG";
    public const string Mpegts = "MPEGTS";
    public const string Mpg = "MPG";
    public const string Mts = "MTS";
    public const string Mvi = "MVI";
    public const string Mxf = "MXF";
    public const string Ogv = "OGV";
    public const string QuickTime = "QuickTime";
    public const string Rm = "RM";
    public const string Rmvb = "RMVB";
    public const string ThreeG2 = "3G2";
    public const string ThreeGp = "3GP";
    public const string Tod = "TOD";
    public const string Ts = "TS";
    public const string Vob = "VOB";
    public const string WebM = "WEBM";
    public const string Wmv = "WMV";
    public const string Wtv = "WTV";

    // Audio Types
    public const string Aac = "AAC";
    public const string Ac3 = "AC3";
    public const string Aiff = "AIFF";
    public const string Amr = "AMR";
    public const string Dts = "DTS";
    public const string Flac = "FLAC";
    public const string M4a = "M4A";
    public const string M4b = "M4B";
    public const string Mp2 = "MP2";
    public const string Mp3 = "MP3";
    public const string Ogg = "OGG";
    public const string Opus = "OPUS";
    public const string Pcm = "PCM";
    public const string Wav = "WAV";
    public const string Wma = "WMA";

    public const string Unknown = "UNKNOWN";

    public static readonly HashSet<string> DateWritableImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // EXIF support
        Jpeg, Heic, Heif, Tiff
    };

    public static readonly HashSet<string> SupportedMixedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Apng, Gif, Webp
    };

    public static readonly HashSet<string> SupportedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Jpeg, Png, Avif, Heic, Heif, Bmp, Tiff
    };

    public static readonly HashSet<string> SupportedVideoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Asf, Avi, Amv, Dv, Dvr_ms, F4V, Flv, Gxf, Lrv,
        M2Ts, M4V, Mj2, Mjpeg, Mkv, Mod, Mov, Mp4, Mpeg,
        Mpegts, Mpg, Mts, Mvi, Mxf, Ogv, Rm, Rmvb, ThreeG2,
        ThreeGp, Tod, Ts, Vob, WebM, Wmv, Wtv,
    };

    public static readonly HashSet<string> SupportedAudioTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Mp3, Wav, Flac, Ogg, Aac, Wma, M4a,  Opus,
        Aiff, Amr, Ac3, Dts, Pcm, M4b, Mp2
    };

    public static readonly HashSet<string> SupportedTypes = new([.. SupportedMixedTypes, .. SupportedImageTypes, .. SupportedVideoTypes, .. SupportedAudioTypes],
        StringComparer.OrdinalIgnoreCase);
}
