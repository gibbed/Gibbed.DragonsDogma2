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
using Gibbed.DragonsDogma2.Common;

namespace Gibbed.DragonsDogma2.FileFormats
{
    public static class FileDetection
    {
        public const int BestGuessLength = 16;

        public static string Guess(Stream input, long length, long fileSize)
        {
            var guessSize = (int)Math.Min(length, BestGuessLength);
            Span<byte> guessSpan = guessSize < 1024
                ? stackalloc byte[guessSize]
                : new byte[guessSize];
            input.ReadToSpan(guessSpan);
            return Guess(guessSpan, fileSize);
        }

        public static string Guess(Span<byte> span, long fileSize)
        {
            if (span.Length == 0)
            {
                return ".null";
            }

            if (
                span.Length >= 4 &&
                span[0] == 'F' &&
                span[1] == 'B' &&
                span[2] == 'F' &&
                span[3] == 'O')
            {
                return ".fbfo";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'F' &&
                span[1] == 'X' &&
                span[2] == 'C' &&
                span[3] == 'T')
            {
                return ".fxct";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'G' &&
                span[1] == 'N' &&
                span[2] == 'P' &&
                span[3] == 'T')
            {
                return ".gnpt";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'M' &&
                span[1] == 'D' &&
                span[2] == 'F' &&
                span[3] == 0)
            {
                return ".mdf";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'M' &&
                span[1] == 'E' &&
                span[2] == 'S' &&
                span[3] == 'H')
            {
                return ".mesh";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'P' &&
                span[1] == 'F' &&
                span[2] == 'B' &&
                span[3] == 0)
            {
                return ".pfb";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'S' &&
                span[1] == 'C' &&
                span[2] == 'N' &&
                span[3] == 0)
            {
                return ".scn";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'S' &&
                span[1] == 'D' &&
                span[2] == 'F' &&
                span[3] == 0)
            {
                return ".sdf";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'S' &&
                span[1] == 'D' &&
                span[2] == 'F' &&
                span[3] == 'T')
            {
                return ".sdft";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'R' &&
                span[1] == 'T' &&
                span[2] == 'E' &&
                span[3] == 'X')
            {
                return ".rtex";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'T' &&
                span[1] == 'E' &&
                span[2] == 'X' &&
                span[3] == 0)
            {
                return ".tex";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'U' &&
                span[1] == 'S' &&
                span[2] == 'R' &&
                span[3] == 0)
            {
                return ".usr";
            }
            else if (
                span.Length >= 4 &&
                span[0] == 'e' &&
                span[1] == 'f' &&
                span[2] == 'x' &&
                span[3] == 'r')
            {
                return ".efx";
            }
            else if (
                span.Length >= 4 &&
                span[0] == '.' &&
                span[1] == 'S' &&
                span[2] == 'V' &&
                span[3] == 'U')
            {
                return ".svu";
            }
            else if (
                span.Length >= 8 &&
                span[4] == 'I' &&
                span[5] == 'F' &&
                span[6] == 'N' &&
                span[7] == 'T')
            {
                return ".ifnt";
            }

            return ".unknown";
        }
    }
}
