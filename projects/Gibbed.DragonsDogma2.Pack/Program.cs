﻿/* Copyright (c) 2025 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Gibbed.DragonsDogma2.Common;
using Gibbed.DragonsDogma2.Common.Hashing;
using Gibbed.DragonsDogma2.FileFormats;
using Gibbed.DragonsDogma2.FileFormats.Packages;
using Gibbed.Memory;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NDesk.Options;

namespace Gibbed.DragonsDogma2.Pack
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            bool compress = false;
            bool verbose = false;
            bool showHelp = false;

            OptionSet options = new()
            {
                { "c|compress", "compress resources", v => compress = v != null },
                { "v|verbose", "be verbose", v => verbose = v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", ProjectHelpers.GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", ProjectHelpers.GetExecutableName());
                return;
            }

            if (extras.Count < 1 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ output_pak input_directory+", ProjectHelpers.GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            List<string> inputPaths = new();
            string outputPath;

            if (extras.Count == 1)
            {
                inputPaths.Add(extras[0]);
                outputPath = Path.ChangeExtension(extras[0], ".pak");
            }
            else
            {
                outputPath = extras[0];
                inputPaths.AddRange(extras.Skip(1));
            }

            SortedDictionary<ulong, PendingEntry> pendingEntries = new();

            if (verbose == true)
            {
                Console.WriteLine("Finding files...");
            }

            var directorySeparatorString = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);
            var altDirectorySeparatorString = Path.AltDirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);

            foreach (var relativePath in inputPaths)
            {
                string inputPath = Path.GetFullPath(relativePath);

                if (inputPath.EndsWith(directorySeparatorString) == true ||
                    inputPath.EndsWith(altDirectorySeparatorString) == true)
                {
                    inputPath = inputPath.Substring(0, inputPath.Length - 1);
                }

                foreach (string path in Directory.EnumerateFiles(inputPath, "*", SearchOption.AllDirectories))
                {
                    PendingEntry pendingEntry;

                    string fullPath = Path.GetFullPath(path);
                    string partPath = fullPath.Substring(inputPath.Length + 1).ToLowerInvariant();

                    pendingEntry.FullPath = fullPath;
                    pendingEntry.PartPath = partPath;

                    var pieces = partPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (pieces.Length >= 2 && string.Compare(pieces[0], "__UNKNOWN", true) == 0)
                    {
                        var partName = Path.GetFileNameWithoutExtension(pieces.Last());

                        if (string.IsNullOrEmpty(partName) == true)
                        {
                            continue;
                        }

                        if (partName.Length > 16)
                        {
                            partName = partName.Substring(0, 16);
                        }

                        pendingEntry.Name = null;

                        if (ulong.TryParse(partName, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out pendingEntry.NameHash) == false)
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    else
                    {
                        var name = pendingEntry.Name = ProjectHelpers.Modifier(string.Join("/", pieces));
                        pendingEntry.NameHash = name.HashFileName();
                    }

                    if (pendingEntries.TryGetValue(pendingEntry.NameHash, out var previousPendingEntry) == true)
                    {
                        Console.WriteLine($"Ignoring duplicate of {pendingEntry.NameHash:X16}: {partPath}");
                        if (verbose == true)
                        {
                            Console.WriteLine($"  Previously added from: {previousPendingEntry.PartPath}");
                        }
                        continue;
                    }

                    pendingEntries[pendingEntry.NameHash] = pendingEntry;
                }
            }

            using (var output = File.Create(outputPath))
            {
                output.Position = PackageFile.EstimateHeaderSize(
                    pendingEntries.Count,
                    false,
                    false,
                    0,
                    false);

                PackageFile package = new()
                {
                    Endian = Endian.Little,
                };

                long current = 0;
                long total = pendingEntries.Count;
                var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                foreach (var pendingEntry in pendingEntries.Select(kv => kv.Value))
                {
                    current++;

                    if (verbose == true)
                    {
                        Console.WriteLine(
                            "[{0}/{1}] {2}",
                            current.ToString(CultureInfo.InvariantCulture).PadLeft(padding),
                            total,
                            pendingEntry.PartPath);
                    }

                    ResourceHeader resource;
                    using (var input = File.OpenRead(pendingEntry.FullPath))
                    {
                        resource = Pack(
                            pendingEntry.NameHash,
                            input, input.Length,
                            ShouldCompress(pendingEntry.Name, compress),
                            output);
                    }
                    package.Resources.Add(resource);
                }

                output.Position = 0;
                package.Serialize(output);
            }
        }

        private static bool ShouldCompress(string name, bool compress) => ExtensionHelper.GetExtension(name) switch
        {
            ".abcmesh" => false,
            ".cmat" => false,
            ".mov" => false,
            ".ncf" => false,
            ".sbnk" => false,
            ".spck" => false,
            ".vsrc" => false,
            _ => compress,
        };

        private static ResourceHeader Pack(ulong nameHash, Stream input, long length, bool compress, Stream output)
        {
            CompressionScheme compressionScheme;
            long dataOffset, dataSizeCompressed;
            ulong dataHash;
            if (compress == true)
            {
                var compressedBytes = CompressZlib(input, length, out dataHash);
                compressionScheme = CompressionScheme.Deflate;
                dataOffset = output.Position;
                dataSizeCompressed = compressedBytes.Length;
                output.Write(compressedBytes, 0, compressedBytes.Length);
            }
            else
            {
                compressionScheme = CompressionScheme.None;
                dataOffset = output.Position;
                dataSizeCompressed = length;
                input.CopyTo(length, uint.MaxValue, output, out dataHash);
            }

            var dataHashBytes = BitConverter.GetBytes(dataHash);
            var dataHashHash = XXH32.Compute(dataHashBytes, uint.MaxValue);

            ResourceHeader resource;
            resource.NameHash = nameHash;
            resource.DataOffset = dataOffset;
            resource.DataSizeUncompressed = length;
            resource.DataSizeCompressed = dataSizeCompressed;
            resource.RawFlags = default;
            resource.DataHash = dataHashHash;
            resource.UnknownHash = 0xCCCCCCCCu;
            resource.CompressionScheme = compressionScheme;
            return resource;
        }

        private static byte[] CompressZlib(Stream input, long length, out ulong dataHash)
        {
            using MemoryStream data = new();
            using DeflaterOutputStream zlib = new(data, new(Deflater.BEST_COMPRESSION, true));
            zlib.IsStreamOwner = false;
            input.CopyTo(length, uint.MaxValue, zlib, out dataHash);
            zlib.Finish();
            zlib.Flush();
            data.Flush();
            return data.ToArray();
        }
    }
}
