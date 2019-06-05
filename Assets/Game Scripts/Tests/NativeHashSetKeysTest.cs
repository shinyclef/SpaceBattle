﻿using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Tests
{
    public class NativeHashSetKeysTest : AssertionHelper
    {
        [Test]
        public void KeysTest()
        {
            var map = new NativeHashMap<int, int>(1000, Allocator.Temp);
            for (int i = 0; i < 500; i++)
            {
                map.TryAdd(i, i);
            }

            var keys = map.GetKeyArray(Allocator.Temp);

            for (int i = 0; i < 500; i++)
            {
                Expect(keys[i] == i);
            }

            keys.Dispose();


            int j = 999;
            for (int i = 500; i < 1000; i++)
            {
                map.TryAdd(i, j);
                j--;
            }

            keys = map.GetKeyArray(Allocator.Temp);

            for (int i = 0; i < 1000; i++)
            {
                Expect(keys[i] == i); // this is not the order I put in!
            }

            map.Dispose();
        }
    }
}