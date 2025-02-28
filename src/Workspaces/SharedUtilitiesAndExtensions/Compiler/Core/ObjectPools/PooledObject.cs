﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// this is RAII object to automatically release pooled object when its owning pool
    /// </summary>
    internal struct PooledObject<T> : IDisposable where T : class
    {
        private readonly Action<ObjectPool<T>, T> _releaser;
        private readonly ObjectPool<T> _pool;
        private T _pooledObject;

        public PooledObject(ObjectPool<T> pool, Func<ObjectPool<T>, T> allocator, Action<ObjectPool<T>, T> releaser) : this()
        {
            _pool = pool;
            _pooledObject = allocator(pool);
            _releaser = releaser;
        }

        public T Object => _pooledObject;

        public void Dispose()
        {
            if (_pooledObject != null)
            {
                _releaser(_pool, _pooledObject);
                _pooledObject = null!;
            }
        }

        #region factory
        public static PooledObject<StringBuilder> Create(ObjectPool<StringBuilder> pool)
        {
            return new PooledObject<StringBuilder>(
                pool,
                Allocator,
                Releaser);
        }

        public static PooledObject<Stack<TItem>> Create<TItem>(ObjectPool<Stack<TItem>> pool)
        {
            return new PooledObject<Stack<TItem>>(
                pool,
                p => Allocator(p),
                (p, sb) => Releaser(p, sb));
        }

        public static PooledObject<Queue<TItem>> Create<TItem>(ObjectPool<Queue<TItem>> pool)
        {
            return new PooledObject<Queue<TItem>>(
                pool,
                p => Allocator(p),
                (p, sb) => Releaser(p, sb));
        }

        public static PooledObject<HashSet<TItem>> Create<TItem>(ObjectPool<HashSet<TItem>> pool)
        {
            return new PooledObject<HashSet<TItem>>(
                pool,
                p => Allocator(p),
                (p, sb) => Releaser(p, sb));
        }

        public static PooledObject<Dictionary<TKey, TValue>> Create<TKey, TValue>(ObjectPool<Dictionary<TKey, TValue>> pool)
            where TKey : notnull
        {
            return new PooledObject<Dictionary<TKey, TValue>>(
                pool,
                p => Allocator(p),
                (p, sb) => Releaser(p, sb));
        }

        public static PooledObject<List<TItem>> Create<TItem>(ObjectPool<List<TItem>> pool)
        {
            return new PooledObject<List<TItem>>(
                pool,
                p => Allocator(p),
                (p, sb) => Releaser(p, sb));
        }
        #endregion

        #region allocators and releasers
        private static StringBuilder Allocator(ObjectPool<StringBuilder> pool)
            => pool.AllocateAndClear();

        private static void Releaser(ObjectPool<StringBuilder> pool, StringBuilder sb)
            => pool.ClearAndFree(sb);

        private static Stack<TItem> Allocator<TItem>(ObjectPool<Stack<TItem>> pool)
            => pool.AllocateAndClear();

        private static void Releaser<TItem>(ObjectPool<Stack<TItem>> pool, Stack<TItem> obj)
            => pool.ClearAndFree(obj);

        private static Queue<TItem> Allocator<TItem>(ObjectPool<Queue<TItem>> pool)
            => pool.AllocateAndClear();

        private static void Releaser<TItem>(ObjectPool<Queue<TItem>> pool, Queue<TItem> obj)
            => pool.ClearAndFree(obj);

        private static HashSet<TItem> Allocator<TItem>(ObjectPool<HashSet<TItem>> pool)
            => pool.AllocateAndClear();

        private static void Releaser<TItem>(ObjectPool<HashSet<TItem>> pool, HashSet<TItem> obj)
            => pool.ClearAndFree(obj);

        private static Dictionary<TKey, TValue> Allocator<TKey, TValue>(ObjectPool<Dictionary<TKey, TValue>> pool)
            where TKey : notnull
            => pool.AllocateAndClear();

        private static void Releaser<TKey, TValue>(ObjectPool<Dictionary<TKey, TValue>> pool, Dictionary<TKey, TValue> obj)
            where TKey : notnull
            => pool.ClearAndFree(obj);

        private static List<TItem> Allocator<TItem>(ObjectPool<List<TItem>> pool)
            => pool.AllocateAndClear();

        private static void Releaser<TItem>(ObjectPool<List<TItem>> pool, List<TItem> obj)
            => pool.ClearAndFree(obj);
        #endregion
    }
}
