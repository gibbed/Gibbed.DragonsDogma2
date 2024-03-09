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
        private readonly List<BlockHeader> _Blocks;

        public PackageFile()
        {
            this._Resources = new();
            this._Blocks = new();
        }

        public Endian Endian { get; set; }
        public bool EncryptResourceHeaders { get; set; }
        public List<ResourceHeader> Resources => this._Resources;
        public UnknownHeader? Unknown { get; set; }
        public uint BlockSize { get; set; }
        public List<BlockHeader> Blocks => this._Blocks;

        public static int EstimateHeaderSize(
            int resourceCount,
            bool hasUnknown,
            int blockCount,
            bool encryptResourceHeaders)
        {
            var resourceHeadersSize = resourceCount * ResourceHeader.HeaderSize;

            var headerSize = FileHeader.HeaderSize;

            headerSize += resourceHeadersSize;

            if (hasUnknown == true)
            {
                headerSize += UnknownHeader.HeaderSize;
            }

            if (blockCount > 0)
            {
                headerSize += BlockTableHeader.HeaderSize;
                headerSize += BlockHeader.HeaderSize * blockCount;
            }

            if (encryptResourceHeaders == true)
            {
                headerSize += 128;
            }

            return headerSize;
        }

        public int EstimateHeaderSize()
        {
            return EstimateHeaderSize(
                this._Resources.Count,
                this.Unknown != null,
                this._Blocks.Count,
                this.EncryptResourceHeaders);
        }

        public void Serialize(Stream output)
        {
            var endian = this.Endian;

            FileFlags fileFlags = FileFlags.None;

            var headerSize = this.EstimateHeaderSize();
            var headerBytes = new byte[headerSize];

            SimpleBufferWriter<byte> headerWriter = new(headerBytes, 0, headerSize);
            headerWriter.Advance(FileHeader.HeaderSize);

            int resourceHeadersSize = ResourceHeader.HeaderSize * this._Resources.Count;
            foreach (var resourceHeader in this._Resources.OrderBy(eh => eh.NameHash))
            {
                resourceHeader.Write(headerWriter, endian);
            }

            if (this.Unknown != null)
            {
                fileFlags |= FileFlags.Unknown2;
                this.Unknown.Value.Write(headerWriter, endian);
            }

            if (this.Blocks.Count > 0)
            {
                fileFlags |= FileFlags.Blocks;
                BlockTableHeader blockTableHeader;
                blockTableHeader.BlockSize = this.BlockSize;
                blockTableHeader.BlockCount = this.Blocks.Count;
                blockTableHeader.Write(headerWriter, endian);
                foreach (var blockHeader in this.Blocks)
                {
                    blockHeader.Write(headerWriter, endian);
                }
            }

            if (this.EncryptResourceHeaders == true)
            {
                fileFlags |= FileFlags.EncryptResourceHeaders;

                var keyBytes = new byte[128];

                Random random = new();
                random.NextBytes(keyBytes);

                var bogocrypt = Bogocrypt.Create(keyBytes);
                bogocrypt.Xor(headerBytes.AsSpan(FileHeader.HeaderSize, resourceHeadersSize));

                headerWriter.WriteBytes(keyBytes);
            }

            FileHeader fileHeader;
            fileHeader.Endian = endian;
            fileHeader.Flags = fileFlags;
            fileHeader.ResourceCount = this._Resources.Count;
            fileHeader.Unknown = 0;

            headerWriter.Reset();
            fileHeader.Write(headerWriter);

            output.Write(headerBytes, 0, headerSize);
        }

        public void Deserialize(Stream input)
        {
            var header = input.ReadToInstance(FileHeader.HeaderSize, FileHeader.Read);
            var endian = header.Endian;

            var unknownHeaderFlags = header.Flags & ~FileFlags.Known;
            if (unknownHeaderFlags != 0)
            {
                throw new FormatException();
            }

            var resourceHeadersSize = header.ResourceCount * ResourceHeader.HeaderSize;
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

            UnknownHeader? unknownHeader;
            if ((header.Flags & FileFlags.Unknown2) != 0)
            {
                unknownHeader = input.ReadToInstance(UnknownHeader.HeaderSize, endian, UnknownHeader.Read);
            }
            else
            {
                unknownHeader = null;
            }

            uint blockSize;
            BlockHeader[] blockHeaders;

            if ((header.Flags & FileFlags.Blocks) != 0)
            {
                var blockTableHeader = input.ReadToInstance(BlockTableHeader.HeaderSize, endian, BlockTableHeader.Read);

                var blockHeadersSize = blockTableHeader.BlockCount * BlockHeader.HeaderSize;
                Span<byte> blockHeaderBuffer = blockHeadersSize < 1024
                    ? stackalloc byte[blockHeadersSize]
                    : new byte[blockHeadersSize];
                input.ReadToSpan(blockHeaderBuffer);

                blockSize = blockTableHeader.BlockSize;
                blockHeaders = new BlockHeader[blockTableHeader.BlockCount];
                int blockHeaderOffset = 0;
                for (int i = 0; i < header.ResourceCount; i++)
                {
                    blockHeaders[i] = BlockHeader.Read(resourceHeaderBuffer, ref blockHeaderOffset, endian);
                }
            }
            else
            {
                blockSize = default;
                blockHeaders = null;
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

            this._Resources.Clear();
            this._Blocks.Clear();

            this.Endian = endian;
            this.EncryptResourceHeaders = (header.Flags & FileFlags.EncryptResourceHeaders) != 0;
            this._Resources.AddRange(resourceHeaders);
            this.Unknown = unknownHeader;
            this.BlockSize = blockSize;
            if (blockHeaders != null)
            {
                this._Blocks.AddRange(blockHeaders);
            }
        }
    }
}
