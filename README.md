# Ed's Media Tools

A collection of Windows drag-and-drop utilities for managing media files. Each tool is a standalone .NET 8 application - just drop files or folders onto the `.exe` to get started.

## Tools

### [Ed's Media Archiver](eds-media-archiver/)

Prepares media files for long-term storage through compression, format standardization, date fixing, and extension correction. Supports 70+ media formats across images, video, and audio.

### [Ed's Media Date Restorer](eds-media-date-restorer/)

Restores original creation dates on media files by extracting dates from EXIF, XMP, filenames, and filesystem metadata, then writing them back consistently across all metadata standards.

### [Ed's Media Tagger](eds-media-tagger/)

AI-powered tagging tool that uses a local LLM (via [Ollama](https://ollama.com/)) to analyze images and videos, then writes descriptive tags directly into file metadata. Fully offline, no cloud APIs.

## Prerequisites

All tools require Windows x64. Additional dependencies vary per tool:

| Dependency | Required By | Install |
|------------|-------------|---------|
| [FFmpeg](https://ffmpeg.org/) | Archiver, Tagger | [Download](https://ffmpeg.org/download.html) |
| [ExifTool](https://exiftool.org/) | Tagger, Date Restorer | [Download](https://exiftool.org/) |
| [Ollama](https://ollama.com/) | Tagger | [Download](https://ollama.com/download) |

## Building from Source

Each tool has its own `.sln` file. Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```sh
cd eds-media-archiver
dotnet publish -c Release
```

Output goes to `bin/Release/net8.0/win-x64/publish/`.

## Contributing

Contributions are welcome! Feel free to open an issue or submit a pull request.
