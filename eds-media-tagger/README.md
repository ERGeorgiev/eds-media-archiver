# Ed's Media Tagger

An AI-powered tool that automatically generates descriptive tags for your images and videos using a local LLM, then writes them directly into the file metadata.

It uses the **Gemma 3 12B** model via [Ollama](https://ollama.com/) to analyze media content and [ExifTool](https://exiftool.org/) to embed tags across multiple metadata standards for broad compatibility.

**This app processes the dropped files directly, so BACKUP IS VERY IMPORTANT!**

![Demo](media/demo.png)

## Features

- Analyzes images and videos using a locally-run AI model (no cloud APIs, fully offline)
- Generates 8-20 descriptive tags covering objects, scenes, colors, activities, mood, and more
- Writes tags to multiple metadata fields (EXIF, XMP, QuickTime) for cross-platform compatibility
- Batch-processes entire directories of media files
- For videos, extracts multiple frames and deduplicates tags for better coverage
- Interactive confirmation before writing tags - you always stay in control
- Choose from multiple vision models depending on your available VRAM

## Supported Formats

| Type   | Extensions                              |
|--------|-----------------------------------------|
| Images | `.jpg`, `.jpeg`, `.png`, `.tiff`, `.tif` |
| Videos | `.mp4`                                  |

## Prerequisites

Make sure the following are installed and available on your `PATH`:

| Dependency | Purpose | Install |
|------------|---------|---------|
| [Ollama](https://ollama.com/) | Local LLM server | [Download](https://ollama.com/download) |
| [FFmpeg](https://ffmpeg.org/) | Video frame extraction | [Download](https://ffmpeg.org/download.html) |
| [ExifTool](https://exiftool.org/) | Metadata writing | [Download](https://exiftool.org/) |

## Usage

Drop files or folders directly onto the compiled `.exe`.

### Workflow

1. The tool scans for supported media files
2. You select which AI model to use (with VRAM requirements shown)
3. Each file is sent to the local model for analysis
4. Generated tags are displayed in the console
5. You are prompted to confirm before tags are written (`Y/n`)
6. Approved tags are embedded into the file's metadata

Press `Ctrl+C` at any time to cancel gracefully.

## How Tags Are Stored

Tags are written to multiple metadata fields to ensure compatibility across different applications:

| Metadata Field | Compatible With |
|----------------|-----------------|
| `XPKeywords` | Windows Explorer |
| `Keywords` | General EXIF readers |
| `Subject` | Adobe Lightroom, digiKam |
| `XMP-dc:Subject` | Cross-platform (XMP standard) |
| `QuickTime:Category` | macOS / iOS |
| `Microsoft:Category` | Windows |

## Building from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```sh
dotnet publish -c Release
```

Output goes to `bin/Release/net8.0/win-x64/publish/`.

## Contributing

Contributions are welcome! Feel free to open an issue or submit a pull request.
