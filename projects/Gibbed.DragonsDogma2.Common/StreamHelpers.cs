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
using System.IO;
using CommunityToolkit.HighPerformance;
using Gibbed.Memory;

namespace Gibbed.DragonsDogma2.Common
{
    public static class StreamHelpers
    {
        public delegate T ReadToInstanceDelegate<T>(ReadOnlySpan<byte> span, ref int index);
        public delegate T ReadToInstanceEndianDelegate<T>(ReadOnlySpan<byte> span, ref int index, Endian endian);

        public static void ReadToSpan(this Stream input, Span<byte> span)
        {
            var read = input.Read(span);
            if (read != span.Length)
            {
                throw new EndOfStreamException();
            }
        }

        public static T ReadToInstance<T>(this Stream input, int size, ReadToInstanceDelegate<T> reader)
        {
            Span<byte> span = size < 1024
                ? stackalloc byte[size]
                : new byte[size];
            var read = input.Read(span);
            if (read != size)
            {
                throw new EndOfStreamException();
            }
            int dummy = 0;
            return reader(span, ref dummy);
        }

        public static T ReadToInstance<T>(this Stream input, int size, Endian endian, ReadToInstanceEndianDelegate<T> reader)
        {
            Span<byte> span = size < 1024
                ? stackalloc byte[size]
                : new byte[size];
            var read = input.Read(span);
            if (read != size)
            {
                throw new EndOfStreamException();
            }
            int dummy = 0;
            return reader(span, ref dummy, endian);
        }
    }
}
