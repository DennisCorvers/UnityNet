using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityNet.Unsafe;

namespace UnityNetTest.UnsafeTests
{
    public class UnsafeArrayTests
    {
        [Test]
        public void ConstructorTest()
        {
            UnsafeArray arr = UnsafeArray.Create<int>(10);

            Assert.AreEqual(arr.Length, 10);
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(0, arr.Get<int>(i));
            }

            arr.Dispose();
        }

        [Test]
        public void DisposeTest()
        {
            UnsafeArray arr = UnsafeArray.Create<int>(10);
            arr.Dispose();
            Assert.AreEqual(true, arr.Disposed);

            arr.Dispose();
        }

        [Test]
        public void InvalidInstanceTest()
        {
            UnsafeArray arr = new UnsafeArray();
            Assert.AreEqual(true, arr.Disposed);
        }

        [Test]
        public void MutateTest()
        {
            UnsafeArray arr = UnsafeArray.Create<int>(10);

            for (int i = 0; i < 10; i++)
            {
                arr.Set(i, i);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, arr.Get<int>(i));
            }

            arr.Dispose();
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            UnsafeArray arr = UnsafeArray.Create<int>(10);

            Assert.Catch(() => { arr.Set<float>(4, 20); });

            arr.Dispose();
        }
#endif

        [Test]
        public void IteratorTest()
        {
            UnsafeArray arr = UnsafeArray.Create<int>(10);

            unsafe
            {
                var itr = UnsafeArray.GetIterator<int>(&arr);
                for (int i = 0; i < 10; i++)
                    arr.Set(i, i * i);

                int num = 0;
                foreach (int i in itr)
                {
                    Assert.AreEqual(num * num, i);
                    num++;
                }
            }
            arr.Dispose();
        }
    }
}
