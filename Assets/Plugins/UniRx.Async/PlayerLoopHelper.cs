#if CSHARP_7_OR_LATER || (UNITY_2018_3_OR_NEWER && (NET_STANDARD_2_0 || NET_4_6))
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using UnityEngine;
using UniRx.Async.Internal;
using System.Threading;

#if UNITY_2019_3_OR_NEWER
using UnityEngine.LowLevel;
using UnityPlayerLoop = UnityEngine.PlayerLoop;
#else
using UnityEngine.Experimental.LowLevel;
using UnityPlayerLoop = UnityEngine.Experimental.PlayerLoop;
#endif

namespace UniRx.Async
{
    public static class UniTaskLoopRunners
    {
        public struct UniTaskLoopRunnerInitialization { };
        public struct UniTaskLoopRunnerEarlyUpdate { };
        public struct UniTaskLoopRunnerFixedUpdate { };
        public struct UniTaskLoopRunnerPreUpdate { };
        public struct UniTaskLoopRunnerUpdate { };
        public struct UniTaskLoopRunnerPreLateUpdate { };
        public struct UniTaskLoopRunnerPostLateUpdate { };

        // Yield

        public struct UniTaskLoopRunnerYieldInitialization { };
        public struct UniTaskLoopRunnerYieldEarlyUpdate { };
        public struct UniTaskLoopRunnerYieldFixedUpdate { };
        public struct UniTaskLoopRunnerYieldPreUpdate { };
        public struct UniTaskLoopRunnerYieldUpdate { };
        public struct UniTaskLoopRunnerYieldPreLateUpdate { };
        public struct UniTaskLoopRunnerYieldPostLateUpdate { };
    }

    public enum PlayerLoopTiming
    {
        Initialization = 0,
        EarlyUpdate = 1,
        FixedUpdate = 2,
        PreUpdate = 3,
        Update = 4,
        PreLateUpdate = 5,
        PostLateUpdate = 6
    }

    public interface IPlayerLoopItem
    {
        bool MoveNext();
    }

    public static class PlayerLoopHelper
    {
        public static SynchronizationContext UnitySynchronizationContext => unitySynchronizationContetext;
        public static int MainThreadId => mainThreadId;

        static int mainThreadId;
        static SynchronizationContext unitySynchronizationContetext;
        static ContinuationQueue[] yielders;
        static PlayerLoopRunner[] runners;

        static PlayerLoopSystem[] InsertRunner(PlayerLoopSystem loopSystem, Type loopRunnerYieldType, ContinuationQueue cq, Type loopRunnerType, PlayerLoopRunner runner)
        {
            var yieldLoop = new PlayerLoopSystem
            {
                type = loopRunnerYieldType,
#if UNITY_EDITOR
                updateDelegate = () =>
                {
                    if (Application.isPlaying)
                    {
                        cq.Run();
                    }
                }
#else
                updateDelegate = cq.Run
#endif
            };

            var runnerLoop = new PlayerLoopSystem
            {
                type = loopRunnerType,
#if UNITY_EDITOR
                updateDelegate = () =>
                {
                    if (Application.isPlaying)
                    {
                        runner.Run();
                    }
                }
#else
                updateDelegate = runner.Run
#endif
            };

            var source = loopSystem.subSystemList ?? new PlayerLoopSystem[0];
            var dest = new PlayerLoopSystem[source.Length + 2];
            Array.Copy(source, 0, dest, 2, source.Length);
            dest[0] = yieldLoop;
            dest[1] = runnerLoop;
            return dest;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            // capture default(unity) sync-context.
            unitySynchronizationContetext = SynchronizationContext.Current;
            mainThreadId = Thread.CurrentThread.ManagedThreadId;

            var playerLoop =
#if UNITY_2019_3_OR_NEWER
                PlayerLoop.GetCurrentPlayerLoop();
#else
                PlayerLoop.GetDefaultPlayerLoop();
#endif

            Initialize(ref playerLoop);
        }

        static readonly Type[] phaseTypes =
        {
            typeof(UnityPlayerLoop.Initialization), typeof(UnityPlayerLoop.EarlyUpdate),
            typeof(UnityPlayerLoop.FixedUpdate), typeof(UnityPlayerLoop.PreUpdate),
            typeof(UnityPlayerLoop.Update), typeof(UnityPlayerLoop.PreLateUpdate),
            typeof(UnityPlayerLoop.PostLateUpdate)
        };

        static readonly Type[] yieldTypes =
        {
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerYieldInitialization),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerYieldEarlyUpdate),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerYieldFixedUpdate),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerYieldPreUpdate),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerYieldUpdate),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerYieldPreLateUpdate),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerYieldPostLateUpdate)
        };

        static readonly Type[] runnerTypes =
        {
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerInitialization),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerEarlyUpdate),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerFixedUpdate),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerPreUpdate),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerUpdate),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerPreLateUpdate),
            typeof(UniTaskLoopRunners.UniTaskLoopRunnerPostLateUpdate)
        };

        // Remove only this legacy UniRx implementation's markers. Cysharp UniTask
        // and Entities have their own types and must retain their nodes/order.
        // Recursion also repairs runners installed in the wrong phase by the old
        // fixed-index implementation on newer Unity versions.
        static PlayerLoopSystem RemoveOwnRunners(PlayerLoopSystem loop)
        {
            var children = loop.subSystemList;
            if (children == null) return loop;
            int count = 0;
            for (int i = 0; i < children.Length; i++)
                if (children[i].type == null || children[i].type.DeclaringType != typeof(UniTaskLoopRunners)) count++;
            var copy = new PlayerLoopSystem[count];
            int next = 0;
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].type != null && children[i].type.DeclaringType == typeof(UniTaskLoopRunners)) continue;
                copy[next++] = RemoveOwnRunners(children[i]);
            }
            loop.subSystemList = copy;
            return loop;
        }

        public static void Initialize(ref PlayerLoopSystem playerLoop)
        {
            // Entities can insert top-level leaves, and Unity can add phases.
            // Resolve all required phases before mutating the loop or queues so
            // an incomplete custom loop produces a useful, atomic failure.
            var nextLoop = RemoveOwnRunners(playerLoop);
            var topLevel = nextLoop.subSystemList;
            var indices = new int[phaseTypes.Length];
            for (int phase = 0; phase < phaseTypes.Length; phase++)
            {
                indices[phase] = -1;
                if (topLevel != null)
                    for (int i = 0; i < topLevel.Length; i++)
                        if (topLevel[i].type == phaseTypes[phase]) { indices[phase] = i; break; }
                if (indices[phase] < 0)
                    throw new InvalidOperationException("UniRx.Async could not find required PlayerLoop phase: " + phaseTypes[phase].FullName);
            }

            // Reinitializing after another package updates the loop must retain
            // pending continuations and avoid installing duplicate callbacks.
            var nextYielders = yielders ?? new ContinuationQueue[phaseTypes.Length];
            var nextRunners = runners ?? new PlayerLoopRunner[phaseTypes.Length];
            for (int phase = 0; phase < phaseTypes.Length; phase++)
            {
                if (nextYielders[phase] == null) nextYielders[phase] = new ContinuationQueue();
                if (nextRunners[phase] == null) nextRunners[phase] = new PlayerLoopRunner();
                int index = indices[phase];
                nextLoop.subSystemList[index].subSystemList = InsertRunner(nextLoop.subSystemList[index],
                    yieldTypes[phase], nextYielders[phase], runnerTypes[phase], nextRunners[phase]);
            }

            PlayerLoop.SetPlayerLoop(nextLoop);
            playerLoop = nextLoop;
            yielders = nextYielders;
            runners = nextRunners;
        }

        public static void AddAction(PlayerLoopTiming timing, IPlayerLoopItem action)
        {
            runners[(int)timing].AddAction(action);
        }

        public static void AddContinuation(PlayerLoopTiming timing, Action continuation)
        {
            yielders[(int)timing].Enqueue(continuation);
        }
    }
}

#endif