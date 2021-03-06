// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FASTER.core
{
    public unsafe partial class FASTERBase
    {
        // Derived class exposed API
        protected void RecoverFuzzyIndex(IndexRecoveryInfo info)
        {
            var ht_version = resizeInfo.version;
            var token = info.token;
            Debug.Assert(state[ht_version].size == info.table_size);

            BeginMainIndexRecovery(ht_version,
                             DirectoryConfiguration.GetPrimaryHashTableFileName(token),
                             info.num_ht_bytes);

            overflowBucketsAllocator.Recover(
                             DirectoryConfiguration.GetOverflowBucketsFileName(token),
                             info.num_buckets,
                             info.num_ofb_bytes);

        }

        internal void RecoverFuzzyIndex(int ht_version, IDevice device, ulong num_ht_bytes, IDevice ofbdevice, int num_buckets, ulong num_ofb_bytes)
        {
            _BeginMainIndexRecovery(ht_version, device, num_ht_bytes);
            overflowBucketsAllocator.Recover(ofbdevice, num_buckets, num_ofb_bytes);
        }

        internal bool IsFuzzyIndexRecoveryComplete(bool waitUntilComplete = false)
        {
            bool completed1 = IsMainIndexRecoveryCompleted(waitUntilComplete);
            bool completed2 = overflowBucketsAllocator.IsRecoveryCompleted(waitUntilComplete);
            return completed1 && completed2;
        }

        //Main Index Recovery Functions
        private int numChunksToBeRecovered;

        private void BeginMainIndexRecovery(
                                        int version,
                                        string filename,
                                        ulong num_bytes)
        {
            _BeginMainIndexRecovery(
                version, 
                new WrappedDevice(new SegmentedLocalStorageDevice(filename, 1L << 30, false, false, true)), 
                num_bytes);
        }

        private void _BeginMainIndexRecovery(
                                int version,
                                IDevice device,
                                ulong num_bytes)
        {
            numChunksToBeRecovered = 1; // Constants.kNumMergeChunks;
            long chunkSize = state[version].size / 1; // Constants.kNumMergeChunks;
            HashBucket* start = state[version].tableAligned;
            uint sizeOfPage = (uint)chunkSize * (uint)sizeof(HashBucket);

            uint num_bytes_read = 0;
            for (int index = 0; index < 1 /*Constants.kNumMergeChunks*/; index++)
            {
                HashBucket* chunkStartBucket = start + (index * chunkSize);
                HashIndexPageAsyncReadResult result = default(HashIndexPageAsyncReadResult);
                result.chunkIndex = index;
                device.ReadAsync(num_bytes_read, (IntPtr)chunkStartBucket, sizeOfPage, AsyncPageReadCallback, result);
                num_bytes_read += sizeOfPage;
            }
            Debug.Assert(num_bytes_read == num_bytes);
        }

        private bool IsMainIndexRecoveryCompleted(
                                        bool waitUntilComplete = false)
        {
            bool completed = (numChunksToBeRecovered == 0);
            if (!completed && waitUntilComplete)
            {
                while (numChunksToBeRecovered != 0)
                {
                    Thread.Sleep(10);
                }
            }
            return completed;
        }

        protected unsafe void AsyncPageReadCallback(
                                        uint errorCode,
                                        uint numBytes,
                                        NativeOverlapped* overlap)
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
                Interlocked.Decrement(ref numChunksToBeRecovered);
            }
        }

        protected void DeleteTentativeEntries()
        {
            HashBucketEntry entry = default(HashBucketEntry);

            int version = resizeInfo.version;
            var table_size_ = state[version].size;
            var ptable_ = state[version].tableAligned;

            for (long bucket = 0; bucket < table_size_; ++bucket)
            {
                HashBucket b = *(ptable_ + bucket);
                while (true)
                {
                    for (int bucket_entry = 0; bucket_entry < Constants.kOverflowBucketIndex; ++bucket_entry)
                    {
                        entry.word = b.bucket_entries[bucket_entry];
                        if (entry.Tentative)
                            b.bucket_entries[bucket_entry] = 0;
                    }

                    if (b.bucket_entries[Constants.kOverflowBucketIndex] == 0) break;
                    b = *((HashBucket*)overflowBucketsAllocator.GetPhysicalAddress((b.bucket_entries[Constants.kOverflowBucketIndex])));
                }
            }
        }
    }
}
