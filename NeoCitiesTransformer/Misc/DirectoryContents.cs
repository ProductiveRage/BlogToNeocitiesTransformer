using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;

namespace NeoCitiesTransformer.Misc
{
    public sealed class DirectoryContents : IDirectoryContents
    {
        private readonly DirectoryInfo _directory;
        public DirectoryContents(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Null/blank/whitespace-only value specified", nameof(path));

            _directory = new DirectoryInfo(path);
        }

        public bool Exists => _directory.Exists;

        public IEnumerator<IFileInfo> GetEnumerator() => _directory.EnumerateFileSystemInfos().Select(item => new FileOrDirectory(item)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class FileOrDirectory : IFileInfo
        {
            private readonly FileSystemInfo _fileOrDirectory;
            public FileOrDirectory(FileSystemInfo fileOrDirectory)
            {
                _fileOrDirectory = fileOrDirectory ?? throw new ArgumentNullException(nameof(fileOrDirectory));
            }

            public bool Exists => _fileOrDirectory.Exists;

            public long Length => (_fileOrDirectory as FileInfo)?.Length ?? throw new NotSupportedException();

            public string PhysicalPath => _fileOrDirectory.FullName;

            public string Name => _fileOrDirectory.Name;

            public DateTimeOffset LastModified => _fileOrDirectory.LastWriteTimeUtc;

            public bool IsDirectory => _fileOrDirectory is DirectoryInfo;

            public Stream CreateReadStream()
            {
                if (_fileOrDirectory is not FileInfo file)
                    throw new NotImplementedException();

                return file.OpenRead();
            }
        }
    }
}