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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibbed.DragonsDogma2.Common;
using Gibbed.DragonsDogma2.FileFormats.Packages;
using Gibbed.Memory;

namespace Gibbed.DragonsDogma2.FileFormats
{
    public class PackageFile
    {
        public const uint Signature = FileHeader.Signature;

        private readonly List<ResourceHeader> _Resources;

        public PackageFile()
        {
            this._Resources = new();
        }

        public Endian Endian { get; set; }
        public bool EncryptResourceHeaders { get; set; }
        public List<ResourceHeader> Resources => this._Resources;

        public int EstimateHeaderSize()
        {
            // TODO(gibbed): other tables
            return FileHeader.Size
                + this._Resources.Count * ResourceHeader.Size;
        }

        public void Serialize(Stream output)
        {
            var endian = this.Endian;

            FileFlags fileFlags = FileFlags.None;

            var entryHeadersSize = this._Resources.Count * ResourceHeader.Size;

            var headerSize = FileHeader.Size + entryHeadersSize;
            var headerBytes = new byte[headerSize];

            SimpleBufferWriter<byte> entryHeadersWriter = new(headerBytes, FileHeader.Size, entryHeadersSize);
            foreach (var entryHeader in this._Resources.OrderBy(eh => eh.NameHash))
            {
                entryHeader.Write(entryHeadersWriter, endian);
            }

            if (this.EncryptResourceHeaders == true)
            {
                var encryptionKeyBytes = new byte[128];

                Random random = new();
                random.NextBytes(encryptionKeyBytes);

                var bogocrypt = Bogocrypt.Create(encryptionKeyBytes);
                bogocrypt.Xor(headerBytes.AsSpan(FileHeader.Size));

                fileFlags |= FileFlags.EncryptResourceHeaders;
            }

            FileHeader fileHeader;
            fileHeader.Endian = endian;
            fileHeader.Flags = fileFlags;
            fileHeader.ResourceCount = this._Resources.Count;
            fileHeader.Unknown = 0;

            SimpleBufferWriter<byte> fileHeaderWriter = new(headerBytes, 0, FileHeader.Size);
            fileHeader.Write(fileHeaderWriter);

            output.Write(headerBytes, 0, headerSize);
        }

        public void Deserialize(Stream input)
        {
            var header = input.ReadToInstance(FileHeader.Size, FileHeader.Read);
            var endian = header.Endian;

            var unknownHeaderFlags = header.Flags & ~FileFlags.Known;
            if (unknownHeaderFlags != 0)
            {
                throw new FormatException();
            }

            var resourceHeadersSize = header.ResourceCount * ResourceHeader.Size;
            Span<byte> resourceHeaderBuffer = resourceHeadersSize < 1024
                ? stackalloc byte[resourceHeadersSize]
                : new byte[resourceHeadersSize];
            input.ReadToSpan(resourceHeaderBuffer);

            if ((header.Flags & FileFlags.Unknown0) != 0)
            {
                // TODO(gibbed): maybe name lookup?
                // byte[16 * header.ResourceCount] <- each entry has pointer into buffer1
                // int buffer1_size
                // byte buffer1[buffer1_size]
                // byte[16 * header.ResourceCount] <- each entry has pointer into buffer2
                // int buffer2_size
                // byte[buffer2_size]
                throw new NotImplementedException();
            }

            if ((header.Flags & FileFlags.Unknown2) != 0)
            {
                // int
                // byte
                // byte
                throw new NotImplementedException();
            }

            if ((header.Flags & FileFlags.Unknown1) != 0)
            {
                // uint
                // uint unknown_count
                // byte[unknown_count * 8]
                throw new NotImplementedException();
            }

            if ((header.Flags & FileFlags.EncryptResourceHeaders) != 0)
            {
                Span<byte> keyBytes = stackalloc byte[128];
                input.ReadToSpan(keyBytes);

                var bogocrypt = Bogocrypt.Create(keyBytes);
                bogocrypt.Xor(resourceHeaderBuffer);
            }

            var resourceHeaders = new ResourceHeader[header.ResourceCount];
            int resourceHeaderOffset = 0;
            for (int i = 0; i < header.ResourceCount; i++)
            {
                resourceHeaders[i] = ResourceHeader.Read(resourceHeaderBuffer, ref resourceHeaderOffset, endian);
            }

            this.Endian = endian;
            this.EncryptResourceHeaders = (header.Flags & FileFlags.EncryptResourceHeaders) != 0;
            this._Resources.Clear();
            this._Resources.AddRange(resourceHeaders);
        }
    }
}
