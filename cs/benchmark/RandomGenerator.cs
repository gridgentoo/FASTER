// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FASTER.core
{
    public class RandomGenerator
    {
        private uint x;
        private uint y;
        private uint z;
        private uint w;

        public RandomGenerator(uint seed = 0)
        {
            if (seed == 0)
            {
                long counter = 0;
                HiResTimer.QueryPerformanceCounter(ref counter);
                x = (uint)(counter & 0x0FFFFFFF);
            }
            else
            {
                x = seed;
            }

            y = 362436069;
            z = 521288629;
            w = 88675123;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Generate()
        {
            uint t;
            t = (x ^ (x << 11));
            x = y;
            y = z;
            z = w;

            return (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Generate(uint max)
        {
            uint t;
            t = (x ^ (x << 11));
            x = y;
            y = z;
            z = w;

            return (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8))) % max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Generate64(ulong max)
        {
            uint t;
            t = (x ^ (x << 11));
            x = y;
            y = z;
            z = w;

            ulong r = (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8)));

            r <<= 32;

            t = (x ^ (x << 11));
            x = y;
            y = z;
            z = w;

            r |= ((w = (w ^ (w >> 19)) ^ (t ^ (t >> 8))));

            return r % max;
        }
    }
}
