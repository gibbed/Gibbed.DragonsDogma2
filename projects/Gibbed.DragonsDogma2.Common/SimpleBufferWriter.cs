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

namespace Gibbed.DragonsDogma2.Common
{
    public sealed class SimpleBufferWriter<T> : IBufferWriter<T>
    {
        private readonly T[] _Buffer;
        private readonly int _Offset;
        private readonly int _Count;
        private int _WriteIndex;
        private int _TotalWriteIndex;

        public SimpleBufferWriter(T[] buffer, int offset, int count)
        {
            this._Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            this._Offset = offset;
            this._Count = count;
            this._WriteIndex = this._TotalWriteIndex = 0;
        }

        public ReadOnlyMemory<T> WrittenMemory => this._Buffer.AsMemory(this._Offset, this._TotalWriteIndex);
        public ReadOnlySpan<T> WrittenSpan => this._Buffer.AsSpan(this._Offset, this._TotalWriteIndex);
        public int WrittenCount => this._TotalWriteIndex;
        public int Capacity => this._Count;
        public int FreeCapacity => this._Count - this._TotalWriteIndex;

        public void Clear()
        {
            this._Buffer.AsSpan(this._Count, this._TotalWriteIndex).Clear();
            this._WriteIndex = this._TotalWriteIndex = 0;
        }

        public void Advance(int count)
        {
            if (count < 0)
            {
                throw new ArgumentException(nameof(count));
            }
            if (this._WriteIndex > this._Count - count)
            {
                throw new InvalidOperationException();
            }
            if (this._WriteIndex == this._TotalWriteIndex)
            {
                this._TotalWriteIndex += count;
            }
            this._WriteIndex += count;
        }

        public void Seek(int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentException(nameof(offset));
            }
            if (offset > this.WrittenCount)
            {
                throw new InvalidOperationException();
            }
            this._WriteIndex = offset;
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            this.CheckBuffer(sizeHint);
            return this._Buffer.AsMemory(this._Offset + this._WriteIndex);
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            CheckBuffer(sizeHint);
            return this._Buffer.AsSpan(this._Offset + this._WriteIndex);
        }

        private void CheckBuffer(int sizeHint)
        {
            if (sizeHint < 0)
            {
                throw new ArgumentException(nameof(sizeHint));
            }

            if (sizeHint > this._Count - this._WriteIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint));
            }
        }
    }
}
