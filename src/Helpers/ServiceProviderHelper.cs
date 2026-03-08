using EdsMediaArchiver.Services;
using EdsMediaArchiver.Services.Compressors;
using EdsMediaArchiver.Services.FileDateReaders;
using EdsMediaArchiver.Services.Logging;
using EdsMediaArchiver.Services.Processors;
using EdsMediaArchiver.Services.Resolvers;
using EdsMediaArchiver.Services.Validators;
using EdsMediaArchiver.Services.Writers;
using Microsoft.Extensions.DependencyInjection;

namespace EdsMediaArchiver.Helpers;

internal static class ServiceProviderHelper
{
    public static IServiceProvider Create()
    {
        var serviceProvider = new ServiceCollection()
            // Compressors
            .AddSingleton<IMediaCompressor, MixedFormatCompressor>()
            .AddSingleton<ImageCompressor>()
            .AddSingleton<IMediaCompressor, ImageCompressor>(x => x.GetRequiredService<ImageCompressor>())
            .AddSingleton<IImageCompressor, ImageCompressor>(x => x.GetRequiredService<ImageCompressor>())
            .AddSingleton<VideoCompressor>()
            .AddSingleton<IMediaCompressor, VideoCompressor>(x => x.GetRequiredService<VideoCompressor>())
            .AddSingleton<IVideoCompressor, VideoCompressor>(x => x.GetRequiredService<VideoCompressor>())
            .AddSingleton<IMediaCompressor, AudioCompressor>()

            // Readers
            .AddSingleton<IOriginalDateReader, OriginalDateReader>()
            .AddSingleton<IFilenameDateReader, FilenameDateReader>()
            .AddSingleton<IOldestDateReader, OldestDateReader>()

            // Logging
            .AddSingleton<IProcessLogger, ProcessLogger>()

            // Processors
            .AddSingleton<ICompressProcessor, CompressProcessor>()
            .AddSingleton<IArchiveProcessor, ArchiveProcessor>()

            // Resolvers
            .AddSingleton<IFileDateResolver, FileDateResolver>()
            .AddSingleton<IFileTypeResolver, FileTypeResolver>()
            .AddSingleton<IFileExtensionResolver, FileExtensionResolver>()

            // Validators
            .AddSingleton<IDateValidator, DateValidator>()

            // Writers
            .AddSingleton<IFileDateWriter, FileDateWriter>()

            // Other
            .AddSingleton<IArchiveRequestFactory, ArchiveRequestFactory>()
            .AddSingleton<IExifToolService, ExifToolService>()

            .BuildServiceProvider();

        return serviceProvider;
    }
}
