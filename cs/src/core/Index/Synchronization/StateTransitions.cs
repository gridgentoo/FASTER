﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FASTER.core
{
    internal enum ResizeOperationStatus : int { IN_PROGRESS, DONE };

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct ResizeInfo
    {
        [FieldOffset(0)]
        public ResizeOperationStatus status;

        [FieldOffset(4)]
        public int version;

        [FieldOffset(0)]
        public long word;
    }

    /// <summary>
    /// The current phase of a state-machine operation such as a checkpoint
    /// </summary>
    public enum Phase : int {
        /// <summary>State machine task has started</summary>
        IN_PROGRESS,

        /// <summary>Waiting for a pending operation to complete</summary>
        WAIT_PENDING,

        /// <summary>Waiting for the index portion of a full checkpoint to complete</summary>
        WAIT_INDEX_CHECKPOINT,

        /// <summary>Waiting for a flush to complete</summary>
        WAIT_FLUSH,

        /// <summary>After flush has completed, write metadata</summary>
        PERSISTENCE_CALLBACK, 

        /// <summary>The default phase; no state-machine operation is happening</summary>
        REST,

        /// <summary>Prepare for an index checkpoint</summary>
        PREP_INDEX_CHECKPOINT,

        /// <summary>Waiting for an index-only checkpoint to complete</summary>
        WAIT_INDEX_ONLY_CHECKPOINT,

        /// <summary>State machine task is being prepared and version is being updated</summary>
        PREPARE,

        /// <summary>State machine is preparing to resize the index</summary>
        PREPARE_GROW,

        /// <summary>Index resizing is in progress</summary>
        IN_PROGRESS_GROW,

        /// <summary></summary>
        INTERMEDIATE = 16,
    };

    /// <summary>
    /// The current state of a state-machine operation such as a checkpoint.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct SystemState
    {
        /// <summary>
        /// The current <see cref="Phase"/> of the operation
        /// </summary>
        [FieldOffset(0)]
        public Phase Phase;

        /// <summary>
        /// The version of the database when this operation is complete
        /// </summary>
        [FieldOffset(4)]
        public int Version;
        
        /// <summary>
        /// The word containing information in bitfields
        /// </summary>
        [FieldOffset(0)]
        public long Word;

        /// <summary>
        /// Copy the <paramref name="other"/> <see cref="SystemState"/> into this <see cref="SystemState"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemState Copy(ref SystemState other)
        {
            var info = default(SystemState);
            info.Word = other.Word;
            return info;
        }

        /// <summary>
        /// Create a <see cref="SystemState"/> with the specified values
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemState Make(Phase status, int version)
        {
            var info = default(SystemState);
            info.Phase = status;
            info.Version = version;
            return info;
        }

        /// <summary>
        /// Create a copy of the passed <see cref="SystemState"/> that is marked with the <see cref="Phase.INTERMEDIATE"/> phase
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemState MakeIntermediate(SystemState state) 
            => Make(state.Phase | Phase.INTERMEDIATE, state.Version);

        /// <summary>
        /// Create a copy of the passed <see cref="SystemState"/> that is not marked with the <see cref="Phase.INTERMEDIATE"/> phase
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveIntermediate(ref SystemState state)
        {
            state.Phase &= ~Phase.INTERMEDIATE;
        }

        /// <summary>
        /// Compare two <see cref="SystemState"/>s for equality
        /// </summary>
        public static bool Equal(SystemState s1, SystemState s2)
        {
            return s1.Word == s2.Word;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{Phase},{Version}]";
        }

        /// <summary>
        /// Compare the current <see cref="SystemState"/> to <paramref name="other"/> for equality
        /// </summary>
        public bool Equals(SystemState other)
        {
            return Word == other.Word;
        }

        /// <summary>
        /// Compare the current <see cref="SystemState"/> to <paramref name="obj"/> for equality if obj is also a <see cref="SystemState"/>
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is SystemState other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Word.GetHashCode();
        }
    }
}
