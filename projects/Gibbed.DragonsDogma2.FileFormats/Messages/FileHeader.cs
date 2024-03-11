﻿/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
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
    internal struct FileHeader
    {
        public const uint Signature = 0x47534D47; // 'GMSG'

        public const int HeaderSize = 16;

        public Endian Endian;
        public int DataOffset;

        public static FileHeader Read(ReadOnlySpan<byte> span, ref int index)
        {
            var magicIndex = index + 4;
            var magic = span.ReadValueU32(ref magicIndex, Endian.Little);
            if (magic != Signature && magic.Swap() != Signature)
            {
                throw new FormatException();
            }
            var endian = magic == Signature ? Endian.Little : Endian.Big;

            var version = span.ReadValueU32(ref index, endian);
            if (version != 22)
            {
                throw new FormatException();
            }
            index += 4;

            FileHeader instance;
            instance.Endian = endian;
            instance.DataOffset = span.ReadValueOffset32(ref index, endian);
            return instance;
        }
    }
}
