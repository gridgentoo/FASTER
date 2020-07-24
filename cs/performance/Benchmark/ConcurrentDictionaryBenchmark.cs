﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable 0162

//#define DASHBOARD

using FASTER.core;
using Performance.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace FASTER.Benchmark
{
    public class KeyComparer : IEqualityComparer<Key>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Key x, Key y)
        {
            return x.value == y.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(Key obj)
        {
            return (int)Utility.GetHashCode(obj.value);
        }
    }

    public unsafe class ConcurrentDictionary_YcsbBenchmark
    {
        public enum Op : ulong
        {
            Upsert = 0,
            Read = 1,
            ReadModifyWrite = 2
        }

        const bool kUseSyntheticData = true;
        const bool kUseSmallData = false;
        const long kInitCount = kUseSmallData ? 2500480 : 250000000;
        const long kTxnCount = kUseSmallData ?  10000000 : 1000000000;
        const int kMaxKey = kUseSmallData ? 1 << 22 : 1 << 28;
        const double theta = 0.99;  // Matches YCSB

        const int kFileChunkSize = 4096;
        const long kChunkSize = 640;

        Key[] init_keys_;

        Key[] txn_keys_;

        long idx_ = 0;

        Input[] input_;
        Input* input_ptr;

        readonly ConcurrentDictionary<Key, Value> store;

        long total_ops_done = 0;

        readonly int threadCount;
        readonly NumaMode numaMode;
        readonly Distribution distribution;
        readonly uint distributionSeed;
        readonly int readPercent;

        internal int runSeconds = Options.DefaultRunSeconds;
        const int kCheckpointSeconds = -1;

        volatile bool done = false;

        public ConcurrentDictionary_YcsbBenchmark(Options options)
        {
            threadCount = options.ThreadCount;
            numaMode = options.GetNumaMode();
            distribution = options.GetDistribution();
            distributionSeed = options.DistributionSeed;
            readPercent = options.ReadPercent;
            runSeconds = options.RunSeconds;

#if DASHBOARD
            statsWritten = new AutoResetEvent[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                statsWritten[i] = new AutoResetEvent(false);
            }
            threadThroughput = new double[threadCount];
            threadAverageLatency = new double[threadCount];
            threadMaximumLatency = new double[threadCount];
            threadProgress = new long[threadCount];
            writeStats = new bool[threadCount];
            freq = Stopwatch.Frequency;
#endif


            store = new ConcurrentDictionary<Key, Value>(threadCount, kMaxKey, new KeyComparer());
        }

        private void RunYcsb(int thread_idx)
        {
            RandomGenerator rng = new RandomGenerator((uint)(1 + thread_idx));

            Numa.AffinitizeThread(numaMode, thread_idx);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Value value = default;
            long reads_done = 0;
            long writes_done = 0;

#if DASHBOARD
            var tstart = Stopwatch.GetTimestamp();
            var tstop1 = tstart;
            var lastWrittenValue = 0;
            int count = 0;
#endif

            while (!done)
            {
                long chunk_idx = Interlocked.Add(ref idx_, kChunkSize) - kChunkSize;
                while (chunk_idx >= kTxnCount)
                {
                    if (chunk_idx == kTxnCount)
                        idx_ = 0;
                    chunk_idx = Interlocked.Add(ref idx_, kChunkSize) - kChunkSize;
                }

                for (long idx = chunk_idx; idx < chunk_idx + kChunkSize && !done; ++idx)
                {
                    Op op;
                    int r = (int)rng.Generate(100);
                    if (r < readPercent)
                        op = Op.Read;
                    else if (readPercent >= 0)
                        op = Op.Upsert;
                    else
                        op = Op.ReadModifyWrite;

                    switch (op)
                    {
                        case Op.Upsert:
                            {
                                store[txn_keys_[idx]] = value;
                                ++writes_done;
                                break;
                            }
                        case Op.Read:
                            {
                                if (store.TryGetValue(txn_keys_[idx], out value))
                                {
                                    ++reads_done;
                                }
                                break;
                            }
                        case Op.ReadModifyWrite:
                            {
                                store.AddOrUpdate(txn_keys_[idx], *(Value*)(input_ptr + (idx & 0x7)), (k, v) => new Value { value = v.value + (input_ptr + (idx & 0x7))->value });
                                ++writes_done;
                                break;
                            }
                        default:
                            throw new InvalidOperationException("Unexpected op: " + op);
                    }
                }

#if DASHBOARD
                count += (int)kChunkSize;

                //Check if stats collector is requesting for statistics
                if (writeStats[thread_idx])
                {
                    var tstart1 = tstop1;
                    tstop1 = Stopwatch.GetTimestamp();
                    threadProgress[thread_idx] = count;
                    threadThroughput[thread_idx] = (count - lastWrittenValue) / ((tstop1 - tstart1) / freq);
                    lastWrittenValue = count;
                    writeStats[thread_idx] = false;
                    statsWritten[thread_idx].Set();
                }
#endif
            }

            sw.Stop();

            Console.WriteLine($"Thread {thread_idx} done; {reads_done:N0} reads," +
                              $" {writes_done:N0} writes, in {sw.ElapsedMilliseconds / 1000.0:N3} sec.");
            Interlocked.Add(ref total_ops_done, reads_done + writes_done);
        }

        public unsafe void Run()
        {
            LoadData();

            input_ = new Input[8];
            for (int i = 0; i < 8; i++)
            {
                input_[i].value = i;
            }
            GCHandle handle = GCHandle.Alloc(input_, GCHandleType.Pinned);
            input_ptr = (Input*)handle.AddrOfPinnedObject();

#if DASHBOARD
            var dash = new Thread(() => DoContinuousMeasurements());
            dash.Start();
#endif

            Thread[] workers = new Thread[threadCount];

            Console.WriteLine("Executing setup.");

            // Setup the store for the YCSB benchmark.
            for (int idx = 0; idx < threadCount; ++idx)
            {
                int x = idx;
                workers[idx] = new Thread(() => SetupYcsb(x));
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Start threads.
            foreach (Thread worker in workers)
            {
                worker.Start();
            }
            foreach (Thread worker in workers)
            {
                worker.Join();
            }
            sw.Stop();

            {
                var ms = sw.ElapsedMilliseconds;
                var sec = ms / 1000.0;
                var upserts_sec = kInitCount / sec;
                Console.WriteLine($"Loading time: {ms}ms ({upserts_sec} upserts/sec)");
            }

            idx_ = 0;

            Console.WriteLine("Executing experiment.");

            // Run the experiment.
            for (int idx = 0; idx < threadCount; ++idx)
            {
                int x = idx;
                workers[idx] = new Thread(() => RunYcsb(x));
            }
            // Start threads.
            foreach (Thread worker in workers)
            {
                worker.Start();
            }

            Stopwatch swatch = new Stopwatch();
            swatch.Start();

            if (kCheckpointSeconds <= 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(runSeconds));
            }
            else
            {
                int runSec = 0;
                while (runSec < this.runSeconds)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(kCheckpointSeconds));
                    runSec += kCheckpointSeconds;
                }
            }

            swatch.Stop();

            done = true;

            foreach (Thread worker in workers)
            {
                worker.Join();
            }

#if DASHBOARD
            dash.Abort();
#endif

            double seconds = swatch.ElapsedMilliseconds / 1000.0;

            Console.WriteLine($"Total {total_ops_done:N0} ops done in {seconds:N3} secs.");
            Console.WriteLine($"##, dist = {distribution}, numa = {numaMode}, read% = {readPercent}, " +
                              $"#threads = {threadCount}, ops/sec = {total_ops_done / seconds:N3}");
        }

        private void SetupYcsb(int thread_idx)
        {
            Numa.AffinitizeThread(numaMode, thread_idx);

#if DASHBOARD
            var tstart = Stopwatch.GetTimestamp();
            var tstop1 = tstart;
            var lastWrittenValue = 0;
            int count = 0;
#endif

            Value value = default;

            for (long chunk_idx = Interlocked.Add(ref idx_, kChunkSize) - kChunkSize;
                chunk_idx < kInitCount;
                chunk_idx = Interlocked.Add(ref idx_, kChunkSize) - kChunkSize)
            {
                for (long idx = chunk_idx; idx < chunk_idx + kChunkSize; ++idx)
                {
                    Key key = init_keys_[idx];
                    store[key] = value;
                }
#if DASHBOARD
                count += (int)kChunkSize;

                //Check if stats collector is requesting for statistics
                if (writeStats[thread_idx])
                {
                    var tstart1 = tstop1;
                    tstop1 = Stopwatch.GetTimestamp();
                    threadThroughput[thread_idx] = (count - lastWrittenValue) / ((tstop1 - tstart1) / freq);
                    lastWrittenValue = count;
                    writeStats[thread_idx] = false;
                    statsWritten[thread_idx].Set();
                }
#endif
            }
        }

#if DASHBOARD
        int measurementInterval = 2000;
        bool allDone;
        bool measureLatency;
        bool[] writeStats;
        private EventWaitHandle[] statsWritten;
        double[] threadThroughput;
        double[] threadAverageLatency;
        double[] threadMaximumLatency;
        long[] threadProgress;
        double freq;

        void DoContinuousMeasurements()
        {

            Numa.AffinitizeThread(numaMode, threadCount + 1);

            double totalThroughput, totalLatency, maximumLatency;
            double totalProgress;
            int ver = 0;

            using (var client = new WebClient())
            {
                while (!allDone)
                {
                    ver++;

                    Thread.Sleep(measurementInterval);

                    totalProgress = 0;
                    totalThroughput = 0;
                    totalLatency = 0;
                    maximumLatency = 0;

                    for (int i = 0; i < threadCount; i++)
                    {
                        writeStats[i] = true;
                    }


                    for (int i = 0; i < threadCount; i++)
                    {
                        statsWritten[i].WaitOne();
                        totalThroughput += threadThroughput[i];
                        totalProgress += threadProgress[i];
                        if (measureLatency)
                        {
                            totalLatency += threadAverageLatency[i];
                            if (threadMaximumLatency[i] > maximumLatency)
                            {
                                maximumLatency = threadMaximumLatency[i];
                            }
                        }
                    }

                    if (measureLatency)
                    {
                        Console.WriteLine("{0} \t {1:0.000} \t {2} \t {3} \t {4} \t {5}", ver, totalThroughput / (double)1000000, totalLatency / threadCount, maximumLatency, store.Count, totalProgress);
                    }
                    else
                    {
                        Console.WriteLine("{0} \t {1:0.000} \t {2} \t {3}", ver, totalThroughput / (double)1000000, store.Count, totalProgress);
                    }
                }
            }
        }
#endif

        #region Load Data

        private void LoadDataFromFile(string filePath)
        {
            string init_filename = filePath + "\\load_" + distribution + "_250M_raw.dat";
            string txn_filename = filePath + "\\run_" + distribution + "_250M_1000M_raw.dat";

            long count = 0;
            using (FileStream stream = File.Open(init_filename, FileMode.Open, FileAccess.Read,
                FileShare.Read))
            {
                Console.WriteLine("loading keys from " + init_filename + " into memory...");
                init_keys_ = new Key[kInitCount];

                byte[] chunk = new byte[kFileChunkSize];
                GCHandle chunk_handle = GCHandle.Alloc(chunk, GCHandleType.Pinned);
                byte* chunk_ptr = (byte*)chunk_handle.AddrOfPinnedObject();

                long offset = 0;

                while (true)
                {
                    stream.Position = offset;
                    int size = stream.Read(chunk, 0, kFileChunkSize);
                    for (int idx = 0; idx < size; idx += 8)
                    {
                        init_keys_[count].value = *(long*)(chunk_ptr + idx);
                        ++count;
                    }
                    if (size == kFileChunkSize)
                        offset += kFileChunkSize;
                    else
                        break;

                    if (count == kInitCount)
                        break;
                }

                if (count != kInitCount)
                {
                    throw new InvalidDataException("Init file load fail!");
                }
            }

            Console.WriteLine("loaded " + kInitCount + " keys.");


            using (FileStream stream = File.Open(txn_filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] chunk = new byte[kFileChunkSize];
                GCHandle chunk_handle = GCHandle.Alloc(chunk, GCHandleType.Pinned);
                byte* chunk_ptr = (byte*)chunk_handle.AddrOfPinnedObject();

                Console.WriteLine("loading txns from " + txn_filename + " into memory...");

                txn_keys_ = new Key[kTxnCount];

                count = 0;
                long offset = 0;

                while (true)
                {
                    stream.Position = offset;
                    int size = stream.Read(chunk, 0, kFileChunkSize);
                    for (int idx = 0; idx < size; idx += 8)
                    {
                        txn_keys_[count] = *((Key*)(chunk_ptr + idx));
                        ++count;
                    }
                    if (size == kFileChunkSize)
                        offset += kFileChunkSize;
                    else
                        break;

                    if (count == kTxnCount)
                        break;
                }

                if (count != kTxnCount)
                {
                    throw new InvalidDataException("Txn file load fail!" + count + ":" + kTxnCount);
                }
            }

            Console.WriteLine("loaded " + kTxnCount + " txns.");
        }

        private void LoadData()
        {
            if (kUseSyntheticData)
            {
                LoadSyntheticData();
                return;
            }

            string filePath = "C:\\ycsb_files";

            if (!Directory.Exists(filePath))
            {
                filePath = "D:\\ycsb_files";
            }
            if (!Directory.Exists(filePath))
            {
                filePath = "E:\\ycsb_files";
            }

            if (Directory.Exists(filePath))
            {
                LoadDataFromFile(filePath);
            }
            else
            {
                Console.WriteLine("WARNING: Could not find YCSB directory, loading synthetic data instead");
                LoadSyntheticData();
            }
        }

        private void LoadSyntheticData()
        {
            Console.WriteLine($"Loading synthetic data ({distribution} distribution)");

            init_keys_ = new Key[kInitCount];
            long val = 0;
            for (int idx = 0; idx < kInitCount; idx++)
            {
                init_keys_[idx] = new Key { value = val++ };
            }

            Console.WriteLine("loaded " + kInitCount + " keys.");

            RandomGenerator generator = new RandomGenerator(this.distributionSeed);

            if (distribution == Distribution.Uniform)
            {
                txn_keys_ = new Key[kTxnCount];
                for (int idx = 0; idx < kTxnCount; idx++)
                {
                    txn_keys_[idx] = new Key { value = (long)generator.Generate64(kInitCount) };
                }
            }
            else if (distribution == Distribution.ZipfSmooth)
            {
                Console.WriteLine("  (zipf (smooth) takes a couple minutes)");
                var zipfSettings = new ZipfSettings
                {
                    Theta = theta,
                    Rng = generator,
                    Shuffle = false,
                    Verbose = false
                };
                txn_keys_ = new Zipf<Key>().GenerateOpKeys(zipfSettings, init_keys_, (int)kTxnCount);
            }
            else
                throw new ArgumentException($"Unknown distribution: {distribution}");

            Console.WriteLine("loaded " + kTxnCount + " txns.");
        }
        #endregion
    }
}