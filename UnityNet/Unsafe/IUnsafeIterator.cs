using System;
using System.Collections.Generic;
using System.Text;

namespace UnityNet.Unsafe
{
    public interface IUnsafeIterator<T> : IEnumerator<T>, IEnumerable<T> where T : struct
    {   }
}
