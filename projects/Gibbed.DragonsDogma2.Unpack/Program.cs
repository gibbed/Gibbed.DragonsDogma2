/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
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
using System.Text.RegularExpressions;
using Gibbed.DragonsDogma2.Common;
using Gibbed.DragonsDogma2.FileFormats;
using Gibbed.DragonsDogma2.FileFormats.Packages;
using NDesk.Options;
using InflaterInputStream = ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream;

namespace Gibbed.DragonsDogma2.Unpack
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            string projectName = null;
            string filterPattern = null;
            bool overwriteFiles = false;
            bool verbose = false;
            bool showHelp = false;

            OptionSet options = new()
            {
                { "p|project=", "set project name", v => projectName = v },
                { "f|filter=", "only extract files using pattern", v => filterPattern = v },
                { "o|overwrite", "overwrite existing files", v => overwriteFiles = v != null },
                { "v|verbose", "be verbose (list files)", v => verbose = v != null },
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

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input [output_directory]", ProjectHelpers.GetExecutableName());
                Console.WriteLine("Unpack specified package.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (string.IsNullOrEmpty(projectName) == true)
            {
                projectName = ProjectHelpers.LoadCurrentProjectName();
            }

            var projectPath = ProjectHelpers.GetProjectPath(projectName);
            if (File.Exists(projectPath) == false)
            {
                Console.WriteLine($"Project file '{projectPath}' is missing!");
                return;
            }

            if (verbose == true)
            {
                Console.WriteLine("Loading project...");
            }

            var project = ProjectData.Project.Load(projectPath);
            if (project == null)
            {
                Console.WriteLine("Failed to load project!");
                return;
            }

            var hashes = project.LoadListsFileNames();

            var inputPath = extras[0];
            string outputBasePath = extras.Count > 1
                ? extras[1]
                : Path.ChangeExtension(inputPath, null) + "_unpack";

            DecompressDelegate GetDecompress(ResourceHeader resource) => resource.CompressionScheme switch
            {
                CompressionScheme.None => DecompressNone,
                CompressionScheme.Deflate => DecompressDeflate,
                _ => throw new NotSupportedException(),
            };

            Regex filter = null;
            if (string.IsNullOrEmpty(filterPattern) == false)
            {
                filter = new(filterPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            using (var input = File.OpenRead(inputPath))
            {
                PackageFile package = new();
                package.Deserialize(input);

                if (package.Blocks.Count > 0)
                {
                    throw new NotImplementedException("support for blocks not yet implemented");
                }

                var groups = package.Resources.GroupBy(rh => rh.UnknownHash).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).ToDictionary(g => g.Key, g => g.ToList());

                long current = 0;
                long total = package.Resources.Count;
                var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                foreach (var resource in package.Resources.OrderBy(eh => eh.DataOffset))
                {
                    current++;

                    var resourceName = hashes[resource.NameHash];
                    if (string.IsNullOrEmpty(resourceName) == true)
                    {
                        var guessResource = resource;
                        var guessDecompress = GetDecompress(guessResource);
                        guessResource.DataSizeUncompressed = Math.Min(guessResource.DataSizeUncompressed, FileDetection.BestGuessLength);
                        using MemoryStream guessData = new();
                        guessDecompress(guessResource, input, guessData);
                        guessData.Flush();

                        guessData.Position = 0;
                        var extension = FileDetection.Guess(guessData, guessData.Length, resource.DataSizeUncompressed);
                        resourceName = $"__UNKNOWN/{resource.NameHash:X16}{extension}";
                    }

                    if (filter != null && filter.IsMatch(resourceName) == false)
                    {
                        continue;
                    }

                    var outputPath = Path.Combine(outputBasePath, resourceName.Replace('/', Path.DirectorySeparatorChar));

                    if (overwriteFiles == false && File.Exists(outputPath) == true)
                    {
                        continue;
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine(
                            "[{0}/{1}] {2}",
                            current.ToString(CultureInfo.InvariantCulture).PadLeft(padding),
                            total,
                            resourceName);
                    }

                    var outputParentPath = Path.GetDirectoryName(outputPath);
                    if (string.IsNullOrEmpty(outputParentPath) == false)
                    {
                        Directory.CreateDirectory(outputParentPath);
                    }

                    var decompress = GetDecompress(resource);

                    using var output = File.Create(outputPath);
                    decompress(resource, input, output);

                    /*
                    using MemoryStream uncompressedData = new();
                    decompress(resource, input, uncompressedData);
                    uncompressedData.Flush();
                    var uncompressedBytes = uncompressedData.ToArray();

                    var uncompressedHash = XXH64.Compute(uncompressedBytes.AsSpan().Slice(0, 64), uint.MaxValue);
                    var uncompressedHashBytes = BitConverter.GetBytes(uncompressedHash);
                    var uncompressedHashHash = XXH32.Compute(uncompressedHashBytes, uint.MaxValue);
                    */
                }
            }
        }

        delegate void DecompressDelegate(ResourceHeader resource, Stream input, Stream output);

        private static void DecompressNone(ResourceHeader resource, Stream input, Stream output)
        {
            input.Position = resource.DataOffset;
            input.CopyTo(resource.DataSizeUncompressed, output);
        }

        private static void DecompressDeflate(ResourceHeader resource, Stream input, Stream output)
        {
            input.Position = resource.DataOffset;
            using InflaterInputStream zlib = new(input, new(true));
            zlib.IsStreamOwner = false;
            zlib.CopyTo(resource.DataSizeUncompressed, output);
        }
    }
}
