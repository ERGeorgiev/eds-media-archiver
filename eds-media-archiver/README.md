# Ed's Media Archiver

A Windows tool that prepares your media files for long-term storage. Drop files or folders onto the `.exe` and choose how to process them - compress, standardize, fix dates, or all of the above.

**This app processes the dropped files directly and often will delete them as it converts them, so BACKUP IS VERY IMPORTANT!**

![Demo](media/demo.png)

## Features

- **Compress**: Converts media to space-efficient formats:
  - Images (PNG, BMP, HEIC, AVIF, TIFF, ...) -> JPG
  - Videos (AVI, MKV, WMV, MOV, 3GP, ...) -> MP4 (H.264 + AAC)
  - Audio (MP3, WAV, FLAC, WMA, ...) -> OGG (Opus)
  - Optional **resize** to max 1920px width/height
- **Standardize**: Normalize formats without aggressive compression (higher quality settings)
- **Set file dates**: Extracts original dates from EXIF, XMP, filenames, and filesystem metadata, then writes them back consistently
- **Fix extensions**: Detects actual file type via magic bytes and corrects mismatched extensions

Supports **70+ media formats** across images, video, and audio.

## Prerequisites

| Dependency | Purpose | Install |
|------------|---------|---------|
| [FFmpeg](https://ffmpeg.org/) | Video/audio processing | [Download](https://ffmpeg.org/download.html) |

FFmpeg must be available on your system `PATH`.

## Usage

1. Download the latest release (`EdsMediaArchiver.exe`)
2. Backup the files and folders you want to process
3. Drag and drop the files and folders onto the `.exe`
4. Answer the interactive prompts to select your options
5. Confirm your backup, and processing begins

Files are processed in parallel with per-file logging and a summary at the end.

> **Warning:** Compression is lossy. PNGs will lose transparency when converted to JPG.

## Building from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```sh
dotnet publish -c Release
```

Output goes to `bin/Release/net8.0/win-x64/publish/`.

## Contributing

Contributions are welcome! Feel free to open an issue or submit a pull request.

Some areas that could use help:
- Transparent PNG handling (currently loses alpha on compress)
- Re-compression detection for already-compressed JPGs
- Animated GIF quality improvements
- Support for additional platforms (Linux, macOS)
