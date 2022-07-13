﻿using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    partial class Queue<T> : IDisposable
    {
        public Queue(T[] items) : this(items.AsSpan(), ArrayPool<T>.Shared)
        { }

        public Queue(T[] items, ArrayPool<T> pool) : this(items.AsSpan(), pool)
        { }

        public Queue(in ReadOnlySpan<T> span) : this(span, ArrayPool<T>.Shared)
        { }

        public Queue(in ReadOnlySpan<T> span, ArrayPool<T> pool)
        {
            _pool = pool ?? ArrayPool<T>.Shared;

            int count = span.Length;

            if (count == 0)
            {
                _array = s_emptyArray;
            }
            else
            {
                _array = _pool.Rent(count);
                span.CopyTo(_array);
                _size = count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest)
            => CopyTo(dest, 0, _size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest, int destIndex)
            => CopyTo(dest, destIndex, _size);

        public void CopyTo(in Span<T> dest, int destIndex, int count)
        {
            if (destIndex < 0 || destIndex > dest.Length)
            {
                ThrowHelper.ThrowDestIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if (count < 0)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            }

            if (dest.Length - destIndex < count || _size < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            int numToCopy = count;
            Span<T> src = _array.AsSpan(0, _size);

            if (src.Length == 0 || numToCopy == 0)
                return;

            int firstPart = Math.Min(src.Length - _head, numToCopy);
            src.Slice(_head, firstPart).CopyTo(dest.Slice(destIndex, firstPart));

            numToCopy -= firstPart;
            if (numToCopy > 0)
            {
                destIndex += src.Length - _head;
                src[..numToCopy].CopyTo(dest.Slice(destIndex, numToCopy));
            }
        }

        public void Dispose()
        {
            ReturnArray(s_emptyArray);
            _head = _tail = _size = 0;
            _version++;
        }
    }
}