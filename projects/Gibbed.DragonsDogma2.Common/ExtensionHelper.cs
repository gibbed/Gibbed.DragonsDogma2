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
using System.Text.RegularExpressions;

namespace Gibbed.DragonsDogma2.Common
{
    public static class ExtensionHelper
    {
        private static readonly Regex InvalidRegex;

        static ExtensionHelper()
        {
            var platforms = string.Join("|", Enum.GetNames(typeof(Platform)));
            var languageCodes = string.Join("|", Enum.GetNames(typeof(LanguageCode)));
            InvalidRegex = new(@$"^\.(?:{platforms}|{languageCodes}|\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public static string GetExtension(string name)
        {
            var regex = InvalidRegex;
            int startIndex = name.Length - 1;
            while (startIndex > 0)
            {
                int position = name.LastIndexOf('.', startIndex);
                if (position < 0)
                {
                    break;
                }
                var length = startIndex - position + 1;
                var match = regex.Match(name, position, length);
                if (match.Success == false)
                {
                    return name.Substring(position, length);
                }
                startIndex = position - 1;
            }
            return null;
        }

        public static string GetFullExtension(string name)
        {
            var regex = InvalidRegex;
            int startIndex = name.Length - 1;
            while (startIndex > 0)
            {
                int position = name.LastIndexOf('.', startIndex);
                if (position < 0)
                {
                    break;
                }
                var length = startIndex - position + 1;
                var match = regex.Match(name, position, length);
                if (match.Success == false)
                {
                    return name.Substring(position);
                }
                startIndex = position - 1;
            }
            return null;
        }

        public static string RemoveExtension(string name)
        {
            var regex = InvalidRegex;
            int startIndex = name.Length - 1;
            while (startIndex > 0)
            {
                int position = name.LastIndexOf('.', startIndex);
                if (position < 0)
                {
                    break;
                }
                var length = startIndex - position + 1;
                var match = regex.Match(name, position, length);
                if (match.Success == false)
                {
                    return name.Substring(0, position);
                }
                startIndex = position - 1;
            }
            return name;
        }
    }
}
