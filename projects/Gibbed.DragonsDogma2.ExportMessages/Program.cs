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
using System.Text;
using Gibbed.DragonsDogma2.Common;
using Gibbed.DragonsDogma2.Common.Hashing;
using Gibbed.DragonsDogma2.FileFormats;
using NDesk.Options;

namespace Gibbed.DragonsDogma2.ExportMessages
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            bool showHelp = false;

            OptionSet options = new()
            {
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ input [output]", ProjectHelpers.GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            const string outputExtension = ".toml";

            var inputBasePath = Path.GetFullPath(extras[0]);

            List<(string inputPath, string outputPath)> targets = new();
            if (Directory.Exists(inputBasePath) == false)
            {
                var inputPath = inputBasePath;
                string outputPath = extras.Count > 1
                    ? Path.GetFullPath(extras[1])
                    : ExtensionHelper.RemoveExtension(inputPath) + outputExtension;
                targets.Add((inputPath, outputPath));
            }
            else
            {
                foreach (var inputPath in Directory.EnumerateFiles(inputBasePath, "*", SearchOption.AllDirectories))
                {
                    if (ExtensionHelper.GetExtension(inputPath) != ".msg")
                    {
                        continue;
                    }
                    var outputPath = ExtensionHelper.RemoveExtension(inputPath) + outputExtension;
                    targets.Add((inputPath, outputPath));
                }
            }

            foreach (var (inputPath, outputPath) in targets.OrderBy(t => t.inputPath))
            {
                var inputBytes = File.ReadAllBytes(inputPath);
                MessageResourceFile messageResource = new();
                messageResource.Deserialize(inputBytes);
                Export(messageResource, outputPath);
            }
        }

        private static void Export(MessageResourceFile messageResource, string outputPath)
        {
            Tommy.TomlArray textArray = new()
            {
                IsTableArray = true,
            };

            foreach (var message in messageResource.Messages)
            {
                Tommy.TomlTable messageTable = new();

                if (message.Guid != Guid.Empty)
                {
                    messageTable["guid"] = message.Guid.ToString();
                }

                if (message.UnknownId != 0)
                {
                    messageTable["unknown_id"] = message.UnknownId;
                }

                if (message.NameHash != 0)
                {
                    uint actualNameHash;
                    if (string.IsNullOrEmpty(message.Name) == false)
                    {
                        var nameBytes = Encoding.Unicode.GetBytes(message.Name);
                        actualNameHash = Murmur3.Compute(nameBytes, uint.MaxValue);
                    }
                    else
                    {
                        actualNameHash = 0;
                    }
                    if (actualNameHash != message.NameHash)
                    {
                        messageTable["name_hash"] = message.NameHash;
                    }
                }

                if (string.IsNullOrEmpty(message.Name) == false)
                {
                    messageTable["name"] = CreateTomlString(message.Name);
                }

                if (message.Texts.Count > 0)
                {
                    Tommy.TomlTable textTable = new();
                    foreach (var kv in message.Texts.OrderBy(kv => kv.Key))
                    {
                        if (string.IsNullOrEmpty(kv.Value) == true)
                        {
                            continue;
                        }
                        textTable[LanguageIdToString(kv.Key)] = CreateTomlString(kv.Value);
                    }
                    messageTable["text"] = textTable;
                }

                textArray.Add(messageTable);
            }

            Tommy.TomlTable rootTable = new();
            rootTable["message"] = textArray;

            StringBuilder sb = new();
            using (StringWriter writer = new(sb))
            {
                rootTable.WriteTo(writer);
                writer.Flush();
            }
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private static string LanguageIdToString(uint id)
        {
            if (Enum.IsDefined(typeof(LanguageCode), id) == true)
            {
                return ((LanguageCode)id).ToString();
            }
            if (Enum.IsDefined(typeof(Language), id) == true)
            {
                return ((Language)id).ToString();
            }
            return id.ToString(CultureInfo.InvariantCulture);
        }

        private static Tommy.TomlString CreateTomlString(string value)
        {
            Tommy.TomlString tomlString = new();
            if (value.Contains("\r") == false && value.Contains("\n") == false)
            {
                tomlString.Value = value;
                if (value.Contains("'") == false)
                {
                    tomlString.PreferLiteral = true;
                }
            }
            else
            {
                tomlString.Value = value;
                tomlString.MultilineTrimFirstLine = true;
                tomlString.IsMultiline = true;
                if (value.Contains("'''") == false)
                {
                    tomlString.PreferLiteral = true;
                }
            }
            return tomlString;
        }
    }
}
