using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityNet.Utils;

namespace UnityNet.Unsafe
{
    public unsafe struct UnsafeArray
    {
        private const string NOT_INITIALIZED = "Array has not been initialized or has been disposed.";
        private const string INVALID_BUFFER_SIZE = "Array size needs to be at least 1.";

        private void* m_buffer;
        private IntPtr m_typeHandle;
        private int m_length;

        public int Length
        {
            get
            {
                UNetDebug.Assert(m_buffer != null);
                return m_length;
            }
        }
        public bool Disposed
        {
            get { return m_buffer == null; }
        }

        public static UnsafeArray Create<T>(int size) where T : unmanaged
        {
            if (size < 1)
                throw new InvalidOperationException(INVALID_BUFFER_SIZE);

            int memSize = size * sizeof(T);

            var array = new UnsafeArray
            {
                m_length = size,
                m_buffer = (void*)Memory.MallocZeroed(memSize),
                m_typeHandle = typeof(T).TypeHandle.Value
            };

            return array;
        }

        public void Dispose()
        {
            Memory.Free((IntPtr)m_buffer);
            m_buffer = null;
        }

        public T* GetPtr<T>(int index) where T : unmanaged
        {
            AssertValid<T>();

            if ((uint)index >= (uint)m_length)
                throw new IndexOutOfRangeException(index.ToString());

            return (T*)m_buffer + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRef<T>(int index) where T : unmanaged
        {
            return ref *GetPtr<T>(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(int index) where T : unmanaged
        {
            return *GetPtr<T>(index);
        }

        public void Set<T>(int index, T value) where T : unmanaged
        {
            AssertValid<T>();

            if ((uint)index >= (uint)m_length)
                throw new IndexOutOfRangeException(index.ToString());

            *((T*)m_buffer + index) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<T>(int index, ref T value) where T : unmanaged
        {
            Set(index, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<T>(int index, T* value) where T : unmanaged
        {
            Set(index, *value);
        }

        public static Iterator<T> GetIterator<T>(UnsafeArray* array) where T : unmanaged
        {
            if (array == null)
                throw new ArgumentNullException("array");

            return new Iterator<T>(array);
        }

        public static void Copy<T>(ref UnsafeArray source, int sourceIndex, ref UnsafeArray destination, int destinationIndex, int count) where T : unmanaged
        {
            UNetDebug.Assert(typeof(T).TypeHandle.Value == source.m_typeHandle);
            UNetDebug.Assert(typeof(T).TypeHandle.Value == destination.m_typeHandle);

            UNetDebug.Assert(source.m_length > sourceIndex);
            UNetDebug.Assert(destination.m_length > destinationIndex);

            UNetDebug.Assert(source.m_length > sourceIndex + count);
            UNetDebug.Assert(destination.m_length > destinationIndex + count);

            Memory.MemCopy(
                (IntPtr)(T*)destination.m_buffer + destinationIndex,
                (IntPtr)(T*)source.m_buffer + sourceIndex,
                count * sizeof(T)
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertValid<T>() where T : unmanaged
        {
            UNetDebug.Assert(m_buffer != null);
            UNetDebug.Assert(typeof(T).TypeHandle.Value == m_typeHandle);
        }


        public unsafe struct Iterator<T> : IUnsafeIterator<T> where T : unmanaged
        {
            private T* m_current;
            private int m_index;
            private UnsafeArray* m_array;

            internal Iterator(UnsafeArray* array)
            {
                m_current = null;
                m_index = -1;
                m_array = array;
            }

            public T Current
            {
                get
                {
                    if (m_current == null)
                        throw new InvalidOperationException();

                    return *m_current;
                }
            }

            public bool MoveNext()
            {
                if (++m_index < m_array->m_length)
                {
                    m_current = m_array->GetPtr<T>(m_index);
                    return true;
                }

                m_current = null;
                return false;
            }

            public void Reset()
            {
                m_index = -1;
                m_current = null;
            }

            public void Dispose()
            { }

            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}
