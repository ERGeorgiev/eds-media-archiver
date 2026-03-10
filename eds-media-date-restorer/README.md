# Ed's Media Date Restorer

A Windows tool that restores and reliably sets the original creation date of your media files. Drop files or folders onto the `.exe` and it will extract the best available date from multiple sources and write it back consistently across all metadata standards.

**This app processes the dropped files directly, so BACKUP IS VERY IMPORTANT!**

## Features

- **Multiple date sources**: Extracts dates from EXIF, XMP, filenames (including Unix timestamps), and filesystem metadata
- **Smart date resolution**: Tries the most reliable source first (EXIF original date), then falls back to filename parsing, then oldest available metadata date
- **Comprehensive writing**: Sets dates across EXIF, XMP, QuickTime, PNG, and Apple metadata standards
- **Timezone preservation**: Stores and restores timezone offset information
- **Filesystem sync**: Updates both file creation and modification times to match
- **Parallel processing**: Handles large batches of files concurrently

### Filename Date Parsing

Recognizes a wide range of date patterns in filenames:

- `20231225`, `2023-12-25`, `2023_12_25`
- `IMG_20231225_143022`
- Short dates like `14-05-12`
- 10-digit Unix timestamps (seconds)
- 13-digit Unix timestamps (milliseconds)

## Prerequisites

| Dependency | Purpose | Install |
|------------|---------|---------|
| [ExifTool](https://exiftool.org/) | Metadata reading/writing | [Download](https://exiftool.org/) |

ExifTool must be available on your system `PATH`.

## Usage

1. Download the latest release (`EdsMediaDateRestorer.exe`)
2. Backup the files and folders you want to process
3. Drag and drop the files and folders onto the `.exe`
4. Confirm your backup, and processing begins

Each file gets its resolved date written to all metadata standards, and filesystem timestamps are updated to match.

## Date Resolution Order

The tool tries these sources in order and uses the first valid result:

1. **EXIF DateTimeOriginal** - the most reliable source for photos
2. **Filename** - parses embedded dates and timestamps from the filename
3. **Oldest available date** - falls back to the oldest date found in any metadata field

## Building from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```sh
dotnet publish -c Release
```

Output goes to `bin/Release/net8.0/win-x64/publish/`.

## Contributing

Contributions are welcome! Feel free to open an issue or submit a pull request.
