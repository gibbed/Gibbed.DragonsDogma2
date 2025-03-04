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

        public const ulong ValidFlags = 0b00000000_00001111_00011101_01111111;
        public const ulong KnownFlags = 0b00000000_00001111_00000000_00001111;

        // 31- 0 ???????? ????cccc ???76s?4 ?321xxxx
        // 63-32 ???????? ???????? ???????? ????????
        // c = crypto scheme
        // x = compression scheme
        // s = maybe is streamed (ie textures)?
        // # = unknown
        public ulong RawFlags;

        public uint DataHash;
        public uint UnknownHash;

        public readonly ulong InvalidFlags => this.RawFlags & ~ValidFlags;
        public readonly ulong UnknownFlags => (this.RawFlags & ValidFlags) & ~KnownFlags;

        public CompressionScheme CompressionScheme
        {
            get { return (CompressionScheme)((this.RawFlags >> 0) & 0xF); }
            set
            {
                const int shift = 0;
                this.RawFlags &= ~(0xFul << shift);
                this.RawFlags |= (((byte)value) & 0xFul) << shift;
            }
        }

        public CryptoSchemeFlags CryptoSchemeFlags
        {
            get { return (CryptoSchemeFlags)((this.RawFlags >> 16) & 0xF); }
            set
            {
                const int shift = 16;
                this.RawFlags &= ~(0xFul << shift);
                this.RawFlags |= (((byte)value) & 0xFul) << shift;
            }
        }

        public CryptoScheme CryptoScheme
        {
            get => this.CryptoSchemeFlags switch
            {
                CryptoSchemeFlags.None => CryptoScheme.None,
                CryptoSchemeFlags.Type1 => CryptoScheme.Type1,
                CryptoSchemeFlags.Type2 => CryptoScheme.Type2,
                CryptoSchemeFlags.Type3 => CryptoScheme.Type3,
                CryptoSchemeFlags.Type4 => CryptoScheme.Type4,
                _ => throw new NotSupportedException(),
            };
            set => this.CryptoSchemeFlags = value switch
            {
                CryptoScheme.None => CryptoSchemeFlags.None,
                CryptoScheme.Type1 => CryptoSchemeFlags.Type1,
                CryptoScheme.Type2 => CryptoSchemeFlags.Type2,
                CryptoScheme.Type3 => CryptoSchemeFlags.Type3,
                CryptoScheme.Type4 => CryptoSchemeFlags.Type4,
                _ => throw new NotSupportedException(),
            };
        }

        internal static ResourceHeader Read(ReadOnlySpan<byte> span, ref int index, Endian endian)
        {
            ResourceHeader instance;
            instance.NameHash = span.ReadValueU64(ref index, endian);
            instance.DataOffset = span.ReadValueS64(ref index, endian);
            instance.DataSizeCompressed = span.ReadValueS64(ref index, endian);
            instance.DataSizeUncompressed = span.ReadValueS64(ref index, endian);
            instance.RawFlags = span.ReadValueU64(ref index, endian);
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
            writer.WriteValueU64(instance.RawFlags, endian);
            writer.WriteValueU32(instance.DataHash, endian);
            writer.WriteValueU32(instance.UnknownHash, endian);
        }

        internal void Write(IBufferWriter<byte> writer, Endian endian)
        {
            Write(this, writer, endian);
        }

        public override string ToString() => $"{this.NameHash:X16} @ {this.DataOffset:X} ({this.DataSizeCompressed}, {this.DataSizeUncompressed}, {this.CompressionScheme}, {this.CryptoScheme}) {this.UnknownFlags:X} {this.DataHash:X8} {this.UnknownHash:X8}";
    }
}
