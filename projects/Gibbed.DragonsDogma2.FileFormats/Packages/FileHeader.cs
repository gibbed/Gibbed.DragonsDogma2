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
    public struct FileHeader
    {
        public const uint Signature = 0x414B504B; // 'AKPK'
        
        public const int HeaderSize = 16;

        public Endian Endian;
        public FileFlags Flags;
        public int ResourceCount;
        public uint Unknown;

        public static FileHeader Read(ReadOnlySpan<byte> span, ref int index)
        {
            var magic = span.ReadValueU32(ref index, Endian.Little);
            if (magic != Signature && magic.Swap() != Signature)
            {
                throw new FormatException();
            }
            var endian = magic == Signature ? Endian.Little : Endian.Big;

            var majorVersion = span.ReadValueU8(ref index);
            var minorVersion = span.ReadValueU8(ref index);
            if (majorVersion != 4 || minorVersion != 1)
            {
                throw new FormatException();
            }

            FileHeader instance;
            instance.Endian = endian;
            instance.Flags = (FileFlags)span.ReadValueU16(ref index, endian);
            instance.ResourceCount = span.ReadValueS32(ref index, endian);
            instance.Unknown = span.ReadValueU32(ref index, endian);
            return instance;
        }

        public static void Write(FileHeader instance, IBufferWriter<byte> writer)
        {
            var endian = instance.Endian;
            writer.WriteValueU32(Signature, endian);
            writer.WriteValueU8(4);
            writer.WriteValueU8(1);
            writer.WriteValueU16((ushort)instance.Flags, endian);
            writer.WriteValueS32(instance.ResourceCount, endian);
            writer.WriteValueU32(instance.Unknown, endian);
        }

        public void Write(IBufferWriter<byte> writer)
        {
            Write(this, writer);
        }
    }
}
