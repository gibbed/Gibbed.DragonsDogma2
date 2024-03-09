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
    public struct UnknownHeader
    {
        public const int HeaderSize = 6;

        public uint Unknown0;
        public byte Unknown4;
        public byte Unknown5;

        internal static UnknownHeader Read(ReadOnlySpan<byte> span, ref int index, Endian endian)
        {
            UnknownHeader instance;
            instance.Unknown0 = span.ReadValueU32(ref index, endian);
            instance.Unknown4 = span.ReadValueU8(ref index);
            instance.Unknown5 = span.ReadValueU8(ref index);
            return instance;
        }

        internal static void Write(UnknownHeader instance, IBufferWriter<byte> writer, Endian endian)
        {
            writer.WriteValueU32(instance.Unknown0, endian);
            writer.WriteValueU8(instance.Unknown4);
            writer.WriteValueU8(instance.Unknown5);
        }

        internal void Write(IBufferWriter<byte> writer, Endian endian)
        {
            Write(this, writer, endian);
        }
    }
}
