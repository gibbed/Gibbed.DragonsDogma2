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
using Gibbed.DragonsDogma2.Common;
using Gibbed.Memory;

namespace Gibbed.DragonsDogma2.FileFormats.Messages
{
    internal struct DataHeader
    {
        public const int HeaderSize = 56;

        public int MessageCount;
        public int ArgumentCount;
        public int LanguageCount;
        public int StringsOffset;
        public int LanguageTableOffset;
        public int ArgumentTypeTableOffset;
        public int ArgumentNameTableOffset;

        public static DataHeader Read(ReadOnlySpan<byte> span, ref int index, Endian endian)
        {
            var startIndex = index;

            DataHeader instance;
            instance.MessageCount = span.ReadValueS32(ref index, endian);
            instance.ArgumentCount = span.ReadValueS32(ref index, endian);
            instance.LanguageCount = span.ReadValueS32(ref index, endian);
            var unknown0C = span.ReadValueU32(ref index, endian);
            instance.StringsOffset = span.ReadValueOffset32(ref index, endian);
            // offset to some data; overwritten with a pointer to the start of the file,
            // so offset in the file is probably just a dummy/end of headers offset
            var unknown18 = span.ReadValueOffset32(ref index, endian);
            instance.LanguageTableOffset = span.ReadValueOffset32(ref index, endian);
            instance.ArgumentTypeTableOffset = span.ReadValueOffset32(ref index, endian);
            instance.ArgumentNameTableOffset = span.ReadValueOffset32(ref index, endian);
            if (unknown0C != 0 || unknown18 != startIndex + HeaderSize + 8 * instance.MessageCount)
            {
                throw new FormatException();
            }
            return instance;
        }
    }
}
