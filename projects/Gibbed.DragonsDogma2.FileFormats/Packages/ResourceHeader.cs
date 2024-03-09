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
    public struct ResourceHeader
    {
        public const int HeaderSize = 48;

        public ulong NameHash;
        public long DataOffset;
        public long DataSizeCompressed;
        public long DataSizeUncompressed;

        // 31- 0 ???????? ????4321 ???????? ????ssss
        // 63-32 ???????? ???????? ???????? ????????
        // s = compression scheme
        // # = unknown
        public ulong Flags;

        public uint DataHash;
        public uint UnknownHash;

        public CompressionScheme CompressionScheme
        {
            get { return (CompressionScheme)(this.Flags & 0xF); }
            set
            {
                this.Flags &= ~0xFul;
                this.Flags |= ((byte)value) & 0xFul;
            }
        }

        internal static ResourceHeader Read(ReadOnlySpan<byte> span, ref int index, Endian endian)
        {
            ResourceHeader instance;
            instance.NameHash = span.ReadValueU64(ref index, endian);
            instance.DataOffset = span.ReadValueS64(ref index, endian);
            instance.DataSizeCompressed = span.ReadValueS64(ref index, endian);
            instance.DataSizeUncompressed = span.ReadValueS64(ref index, endian);
            instance.Flags = span.ReadValueU64(ref index, endian);
            instance.DataHash = span.ReadValueU32(ref index, endian);
            instance.UnknownHash = span.ReadValueU32(ref index, endian);
            return instance;
        }

        internal static void Write(ResourceHeader instance, IBufferWriter<byte> writer, Endian endian)
        {
            writer.WriteValueU64(instance.NameHash, endian);
            writer.WriteValueS64(instance.DataOffset, endian);
            writer.WriteValueS64(instance.DataSizeCompressed, endian);
            writer.WriteValueS64(instance.DataSizeUncompressed, endian);
            writer.WriteValueU64(instance.Flags, endian);
            writer.WriteValueU32(instance.DataHash, endian);
            writer.WriteValueU32(instance.UnknownHash, endian);
        }

        internal void Write(IBufferWriter<byte> writer, Endian endian)
        {
            Write(this, writer, endian);
        }

        public override string ToString() => $"{this.NameHash:X16} @ {this.DataOffset:X} ({this.DataSizeCompressed}, {this.DataSizeUncompressed}, {this.CompressionScheme}) {this.Flags >> 4} {this.DataHash:X8} {this.UnknownHash:X8}";
    }
}
