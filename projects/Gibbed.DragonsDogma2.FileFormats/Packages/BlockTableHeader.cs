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
using System.Buffers;
using Gibbed.Memory;

namespace Gibbed.DragonsDogma2.FileFormats.Packages
{
    public struct BlockTableHeader
    {
        public const int HeaderSize = 8;

        public uint BlockSize;
        public int BlockCount;

        internal static BlockTableHeader Read(ReadOnlySpan<byte> span, ref int index, Endian endian)
        {
            BlockTableHeader instance;
            instance.BlockSize = span.ReadValueU32(ref index, endian);
            instance.BlockCount = span.ReadValueS32(ref index, endian);
            return instance;
        }

        internal static void Write(BlockTableHeader instance, IBufferWriter<byte> writer, Endian endian)
        {
            writer.WriteValueU32(instance.BlockSize, endian);
            writer.WriteValueS32(instance.BlockCount, endian);
        }

        internal void Write(IBufferWriter<byte> writer, Endian endian)
        {
            Write(this, writer, endian);
        }
    }
}
