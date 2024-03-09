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
using System.Buffers;
using Gibbed.Memory;

namespace Gibbed.DragonsDogma2.FileFormats.Packages
{
    public struct BlockHeader
    {
        public const int HeaderSize = 8;

        private const int OffsetShift = 0;
        private const ulong OffsetMask = 0x000003FF_FFFFFFFFul;

        private const int SizeShift = 42;
        private const ulong SizeMask = 0x00000000_00FFFFFCul;

        // 31- 0 oooooooo oooooooo oooooooo oooooooo
        // 63-32 ssssssss ssssssss ssssssoo oooooooo
        // o = offset
        // s = size

        public ulong Flags;

        public long Offset
        {
            readonly get => (long)((this.Flags >> OffsetShift) & OffsetMask);
            set
            {
                this.Flags &= ~(OffsetMask << OffsetShift);
                this.Flags |= ((ulong)value & OffsetMask) << OffsetShift;
            }
        }

        public int Size
        {
            readonly get => (int)((this.Flags >> SizeShift) & SizeMask);
            set
            {
                this.Flags &= ~(SizeMask << SizeShift);
                this.Flags |= ((ulong)value & SizeMask) << SizeShift;
            }
        }

        internal static BlockHeader Read(ReadOnlySpan<byte> span, ref int index, Endian endian)
        {
            BlockHeader instance;
            instance.Flags = span.ReadValueU64(ref index, endian);
            return instance;
        }

        internal static void Write(BlockHeader instance, IBufferWriter<byte> writer, Endian endian)
        {
            writer.WriteValueU64(instance.Flags, endian);
        }

        internal void Write(IBufferWriter<byte> writer, Endian endian)
        {
            Write(this, writer, endian);
        }
    }
}
