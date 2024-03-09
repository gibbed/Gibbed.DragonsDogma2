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
using XXH64 = K4os.Hash.xxHash.XXH64;

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

        public static void CopyTo(this Stream input, long size, Stream output, int bufferSize)
        {
            long left = size;
            var buffer = new byte[bufferSize];
            while (left > 0)
            {
                var blockSize = (int)Math.Min(left, bufferSize);
                var read = input.Read(buffer, 0, blockSize);
                if (read != blockSize)
                {
                    throw new EndOfStreamException();
                }
                output.Write(buffer, 0, blockSize);
                left -= blockSize;
            }
        }

        public static void CopyTo(this Stream input, long size, Stream output)
        {
            CopyTo(input, size, output, 0x40000);
        }

        public static void CopyTo(
            this Stream input, long size,
            uint hashSeed,
            int bufferSize,
            Stream output,
            out ulong hash)
        {
            XXH64.State state = default;
            XXH64.Reset(ref state, hashSeed);
            long left = size;
            var buffer = new byte[bufferSize];
            while (left > 0)
            {
                var blockSize = (int)Math.Min(left, bufferSize);
                var readSize = input.Read(buffer, 0, blockSize);
                if (readSize != blockSize)
                {
                    throw new EndOfStreamException();
                }
                XXH64.Update(ref state, new ReadOnlySpan<byte>(buffer, 0, blockSize));
                output.Write(buffer, 0, blockSize);
                left -= blockSize;
            }
            hash = XXH64.Digest(state);
        }

        public static void CopyTo(
            this Stream input,
            uint hashSeed,
            Stream output,
            out ulong hash)
        {
            CopyTo(input, input.Length, hashSeed, 0x40000, output, out hash);
        }

        public static void CopyTo(
            this Stream input, long size,
            uint hashSeed,
            Stream output,
            out ulong hash)
        {
            CopyTo(input, size, hashSeed, 0x40000, output, out hash);
        }
    }
}
