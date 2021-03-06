// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Threading;

namespace FASTER.core
{
    public unsafe struct Empty
    {
        public static Empty* MoveToContext(Empty* empty)
        {
            return empty;
        }
    }


    public static class Utility
    {
        /// <summary>
        /// Helper function used to check if two keys are equal
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsEqual(byte* src, byte* dest)
        {
            if (*(int*)src == *(int*)dest)
            {
                for (int i = 0; i < *(int*)src; i++)
                {
                    if (*(src + 4 + i) != *(dest + 4 + i))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsEqual(byte* src, byte* dst, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (*(src + i) != *(dst + i))
                {
                    return false;
                }
            }
            return true;
        }

        public unsafe static void Copy(byte* src, byte* dest, int numBytes)
        {
            for(int i = 0; i < numBytes; i++)
            {
                *(dest + i) = *(src + i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetHashCode(long input)
        {
            long local_rand = input;
            long local_rand_hash = 8;

            local_rand_hash = 40343 * local_rand_hash + ((local_rand) & 0xFFFF);
            local_rand_hash = 40343 * local_rand_hash + ((local_rand >> 16) & 0xFFFF);
            local_rand_hash = 40343 * local_rand_hash + ((local_rand >> 32) & 0xFFFF);
            local_rand_hash = 40343 * local_rand_hash + (local_rand >> 48);
            local_rand_hash = 40343 * local_rand_hash;

            return (long)Rotr64((ulong)local_rand_hash, 45);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long HashBytes(byte* pbString, int len)
        {
            const long magicno = 40343;
            char* pwString = (char*)pbString;
            int cbBuf = len / 2;
            ulong hashState = (ulong)len;

            for (int i = 0; i < cbBuf; i++, pwString++)
                hashState = magicno * hashState + (ulong)*pwString;

            if ((len & 1) > 0)
            {
                char* pC = (char*)pwString;
                hashState = magicno * hashState + (ulong)*pC;
            }

            return (long)Rotr64(magicno * hashState, 4);
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Rotr64(ulong x, int n)
        {
            return (((x) >> n) | ((x) << (64 - n)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(long x)
        {
            return (x > 0) && ((x & (x - 1)) == 0);
        }

        static readonly int[] MultiplyDeBruijnBitPosition2 = new int[32]
        {
            0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
            31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLogBase2(int x)
        {
            return MultiplyDeBruijnBitPosition2[(uint)(x * 0x077CB531U) >> 27];
        }

        public static int GetLogBase2(UInt64 value)
        {
            int i;
            for (i = -1; value != 0; i++)
                value >>= 1;

            return (i == -1) ? 0 : i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is32Bit(long x)
        {
            return ((ulong)x < 4294967295ul);
        }

        /// <summary>
        /// Finds the first bit. Returns the "index" of the first bit that is 1 in the
        /// given value starting from the least significant bit. Returns 0 if value
        /// is 0.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FindFirstBitSet(ulong value)
        {
            if (value == 0)
            {
                return 0;
            }

            ulong bit = 1;
            uint checks = 1;
            while ((value & bit) == 0)
            {
                bit <<= 1;
                ++checks;
            }
            return checks;
        }

        /// <summary>
        /// Turn on the given bit specified by mask in the target.
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long TurnOnBit(long mask, long target)
        {
            return target |= mask;
        }

        /// <summary>
        /// Turn on the given bit specified by mask in the target.
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong TurnOnBit(ulong mask, ulong target)
        {
            return target |= mask;
        }

        /// <summary>
        /// A 32-bit murmur3 implementation.
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        public static int Murmur3(int h)
        {
            uint a = (uint)h;
            a ^= a >> 16;
            a *= 0x85ebca6b;
            a ^= a >> 13;
            a *= 0xc2b2ae35;
            a ^= a >> 16;
            return (int)a;
        }

        public static unsafe void AsyncCountdownCallback(uint errorCode, uint numBytes, NativeOverlapped* overlap)
        {
            try
            {
                if (errorCode != 0)
                {
                    System.Diagnostics.Trace.TraceError("OverlappedStream GetQueuedCompletionStatus error: {0}", errorCode);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Completion Callback error, {0}", ex.Message);
            }
            finally
            {
                CountdownEventAsyncResult result = (CountdownEventAsyncResult)Overlapped.Unpack(overlap).AsyncResult;
                if(result.countdown == null)
                {
                    throw new Exception("Countdown event cannot be null!");
                }
                result.countdown.Signal();
                if(result.countdown.IsSet)
                {
                    result.action();
                }
                Overlapped.Free(overlap);
            }
        }
    }
}
