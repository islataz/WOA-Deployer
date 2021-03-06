﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Deployer.Core.Exceptions;
using Serilog;
using Zafiro.Core.Patterns.Either;

namespace Deployer.Core.Services.Wim
{
    public abstract class WindowsImageMetadataReaderBase : IWindowsImageMetadataReader
    {
        private static XmlSerializer Serializer { get; } = new XmlSerializer(typeof(WimMetadata));

        public Either<ErrorList, XmlWindowsImageMetadata> Load(Stream stream)
        {
            Log.Verbose("Getting WIM stream");

            WimMetadata metadata;
            try
            {
                metadata = (WimMetadata)Serializer.Deserialize(GetXmlMetadataStream(stream));
            }
            catch (Exception e)
            {
                return new ErrorList("Could not read the metadata from the WIM " +
                                     $"file. Please, check it's a valid .WIM file: {e.Message}");
            }

            Log.Verbose("Wim metadata deserialized correctly {@Metadata}", metadata);

            return new XmlWindowsImageMetadata
            {
                Images = metadata.Images
                    .Where(x => x.Windows != null)
                    .Select(x => new DiskImageMetadata
                {
                    Architecture = GetArchitecture(x.Windows.Arch).Handle(list => MyArchitecture.Unknown),
                    Build = x.Windows.Version.Build,
                    DisplayName = x.Name,
                    Index = int.Parse(x.Index)
                }).ToList()
            };
        }

        private static Either<ErrorList, MyArchitecture> GetArchitecture(string str)
        {
            switch (str)
            {
                case "0":
                    return MyArchitecture.X86;
                case "9":
                    return MyArchitecture.X64;
                case "12":
                    return MyArchitecture.Arm64;
            }

            throw new IndexOutOfRangeException($"The architecture '{str}' is unknown");
        }

        protected abstract Stream GetXmlMetadataStream(Stream wim);
    }

    public enum MyArchitecture
    {
        X86,
        X64,
        Arm64,
        Unknown
    }
}