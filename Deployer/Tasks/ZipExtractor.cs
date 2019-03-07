﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace Deployer.Tasks
{
    public class ZipExtractor : IZipExtractor
    {
        private readonly IFileSystemOperations fileSystemOperations;

        public ZipExtractor(IFileSystemOperations fileSystemOperations)
        {
            this.fileSystemOperations = fileSystemOperations;
        }

        public async Task ExtractFirstChildToFolder(Stream stream, string destination,
            IObserver<double> progressObserver = null)
        {
            await ExtractCore(stream, destination,
                zipArchive =>
                {
                    var baseEntry = FirstChild(zipArchive.Entries);
                    var contents = zipArchive.Entries.Where(x => x.Key.StartsWith(baseEntry.Key) && !x.IsDirectory);
                    return contents;
                }, FirstChild, progressObserver);
        }

        public async Task ExtractToFolder(Stream stream, string destination, IObserver<double> progressObserver = null)
        {
            await ExtractCore(stream, destination,
                zipArchive =>
                {
                    var contents = zipArchive.Entries.Where(x => !x.IsDirectory);
                    return contents;
                }, progressObserver: progressObserver);
        }

        public async Task ExtractRelativeFolder(Stream stream, string relativeZipPath, string destination,
            IObserver<double> progressObserver = null)
        {
            await ExtractCore(stream, destination,
                zipArchive =>
                {
                    var baseEntry = RootPath(relativeZipPath);
                    var contents = zipArchive.Entries.Where(x => x.Key.StartsWith(baseEntry) && !x.IsDirectory);
                    return contents;
                }, progressObserver: progressObserver);
        }

        public async Task ExtractRelativeFolder(Stream stream, Func<IEnumerable<ZipArchiveEntry>, ZipArchiveEntry> getRelative, string destination, IObserver<double> progressObserver = null)
        {
            await ExtractCore(stream, destination,
                zipArchive =>
                {
                    var baseEntry = RootPath(getRelative(zipArchive.Entries).Key);
                    var contents = zipArchive.Entries.Where(x => x.Key.StartsWith(baseEntry) && !x.IsDirectory);
                    return contents;
                }, progressObserver: progressObserver);
        }

        private async Task ExtractCore(Stream stream, string destination,
            Func<ZipArchive, IEnumerable<ZipArchiveEntry>> selectEntries,
            Func<IEnumerable<ZipArchiveEntry>, ZipArchiveEntry> baseEntry = null, IObserver<double> progressObserver = null)
        {
            var archive = ZipArchive.Open(stream);
            var entries = selectEntries(archive);
            await ExtractContents(entries.ToList(), destination, baseEntry?.Invoke(archive.Entries), progressObserver);
        }

        private async Task ExtractContents(ICollection<ZipArchiveEntry> entries, string destination,
            IEntry baseEntry = null, IObserver<double> progressObserver = null)
        {
            var total = entries.Count;
            var copied = 0;

            foreach (var entry in entries)
            {
                var filePath = entry.Key.Substring(baseEntry?.Key.Length ?? 0);

                var destFile = Path.Combine(destination, filePath.Replace("/", "\\"));
                var dir = Path.GetDirectoryName(destFile);
                if (!fileSystemOperations.DirectoryExists(dir))
                {
                    fileSystemOperations.CreateDirectory(dir);
                }

                using (var destStream = File.Open(destFile, FileMode.OpenOrCreate))
                using (var stream = entry.OpenEntryStream())
                {
                    await stream.CopyToAsync(destStream);
                    copied++;
                    progressObserver?.OnNext(copied / (double)total);
                }
            }

            progressObserver?.OnNext(double.NaN);
        }

        private ZipArchiveEntry FirstChild(IEnumerable<ZipArchiveEntry> zipArchiveEntries)
        {
            return zipArchiveEntries.First(x => x.IsDirectory);
        }

        private static string RootPath(string relativeZipPath)
        {
            relativeZipPath = relativeZipPath.Replace('\\', '/');
            return relativeZipPath.EndsWith("//") ? relativeZipPath : relativeZipPath + "/";
        }
    }
}