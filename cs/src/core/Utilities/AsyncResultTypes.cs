// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#define CALLOC

using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FASTER.core
{
    public static class Config
    {
        //public static string CheckpointDirectory = Path.GetTempPath() + "fasterlogs";
        public static string CheckpointDirectory = "D:\\data";
    }

    public struct AsyncGetFromDiskResult<TContext> : IAsyncResult
    {
        //public SectorAlignedMemory record;
        //public SectorAlignedMemory objBuffer;
        public TContext context;

        public bool IsCompleted => throw new NotImplementedException();

        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public object AsyncState => throw new NotImplementedException();

        public bool CompletedSynchronously => throw new NotImplementedException();
    }

    public class PageAsyncFlushResult : IAsyncResult
    {
        public long page;
        public bool partial;
        public long untilAddress;
        public int count;
        public CountdownEvent handle;
        public ISegmentedDevice objlogDevice;
        public SectorAlignedMemory freeBuffer1;
        public SectorAlignedMemory freeBuffer2;

        public bool IsCompleted => throw new NotImplementedException();

        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public object AsyncState => throw new NotImplementedException();

        public bool CompletedSynchronously => throw new NotImplementedException();
    }

    public struct PageAsyncReadResult<TContext> : IAsyncResult
    {
        public long page;
        public TContext context; 

        public bool IsCompleted => throw new NotImplementedException();

        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public object AsyncState => throw new NotImplementedException();

        public bool CompletedSynchronously => throw new NotImplementedException();
    }

    public struct PageAsyncFlushResult<TContext> : IAsyncResult
    {
        public long page;
        public TContext context;

        public bool IsCompleted => throw new NotImplementedException();

        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public object AsyncState => throw new NotImplementedException();

        public bool CompletedSynchronously => throw new NotImplementedException();
    }

    public unsafe class HashIndexPageAsyncFlushResult : IAsyncResult
    {
        public HashBucket* start;
        public int numChunks;
        public int numIssued;
        public int numFinished;
        public uint chunkSize;
        public IDevice device;
        public Stopwatch sw;

        public bool IsCompleted => throw new NotImplementedException();

		public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

		public object AsyncState => throw new NotImplementedException();

		public bool CompletedSynchronously => throw new NotImplementedException();
	}

    public struct HashIndexPageAsyncReadResult : IAsyncResult
    {
        public int chunkIndex;

        public bool IsCompleted => throw new NotImplementedException();

        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public object AsyncState => throw new NotImplementedException();

        public bool CompletedSynchronously => throw new NotImplementedException();
    }

    public struct OverflowPagesFlushAsyncResult : IAsyncResult
    {
        public bool IsCompleted => throw new NotImplementedException();

        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public object AsyncState => throw new NotImplementedException();

        public bool CompletedSynchronously => throw new NotImplementedException();
    }

    public struct OverflowPagesReadAsyncResult : IAsyncResult
    {

        public bool IsCompleted => throw new NotImplementedException();

        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public object AsyncState => throw new NotImplementedException();

        public bool CompletedSynchronously => throw new NotImplementedException();
    }

    public struct CountdownEventAsyncResult : IAsyncResult
    {
        public CountdownEvent countdown;
        public Action action;

        public bool IsCompleted => throw new NotImplementedException();

        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public object AsyncState => throw new NotImplementedException();

        public bool CompletedSynchronously => throw new NotImplementedException();
    }
}
