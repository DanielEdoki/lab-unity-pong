using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using GameSystemsCookbook;
using GameSystemsCookbook.Demos.PaddleBall;

namespace GameSystemsCookbook.Tests
{
    /// <summary>
    /// EditMode tests covering scoring, victory detection, and event channel behavior.
    /// </summary>
    public class ScoringAndGameLogicTests
    {
        // ── Score Tests ──

        [Test]
        public void Score_IncrementScore_IncreasesValueByOne()
        {
            var score = new Score();

            score.IncrementScore();

            Assert.AreEqual(1, score.Value);
        }

        [Test]
        public void Score_MultipleIncrements_AccumulatesCorrectly()
        {
            var score = new Score();

            score.IncrementScore();
            score.IncrementScore();
            score.IncrementScore();

            Assert.AreEqual(3, score.Value);
        }

        [Test]
        public void Score_ResetScore_SetsValueToZero()
        {
            var score = new Score();
            score.IncrementScore();
            score.IncrementScore();

            score.ResetScore();

            Assert.AreEqual(0, score.Value);
        }

        [Test]
        public void Score_InitialValue_IsZero()
        {
            var score = new Score();

            Assert.AreEqual(0, score.Value);
        }

        // ── Event Channel Tests ──

        [Test]
        public void VoidEventChannel_RaiseEvent_NotifiesSubscribers()
        {
            var channel = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            bool wasInvoked = false;
            channel.OnEventRaised += () => wasInvoked = true;

            channel.RaiseEvent();

            Assert.IsTrue(wasInvoked);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void VoidEventChannel_RaiseEvent_WithNoSubscribers_DoesNotThrow()
        {
            var channel = ScriptableObject.CreateInstance<VoidEventChannelSO>();

            Assert.DoesNotThrow(() => channel.RaiseEvent());
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void IntEventChannel_RaiseEvent_PassesCorrectPayload()
        {
            var channel = ScriptableObject.CreateInstance<IntEventChannelSO>();
            int receivedValue = -1;
            channel.OnEventRaised += (value) => receivedValue = value;

            channel.RaiseEvent(42);

            Assert.AreEqual(42, receivedValue);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void VoidEventChannel_Unsubscribe_StopsReceivingEvents()
        {
            var channel = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            int invokeCount = 0;
            UnityEngine.Events.UnityAction handler = () => invokeCount++;

            channel.OnEventRaised += handler;
            channel.RaiseEvent();
            channel.OnEventRaised -= handler;
            channel.RaiseEvent();

            Assert.AreEqual(1, invokeCount);
            Object.DestroyImmediate(channel);
        }

        // ── Victory Detection Tests ──

        [Test]
        public void ScoreObjective_PlayerReachesTarget_RaisesTargetScoreReachedEvent()
        {
            // Create all required SOs
            var scoreObjective = ScriptableObject.CreateInstance<ScoreObjectiveSO>();
            var scoreManagerUpdated = ScriptableObject.CreateInstance<ScoreListEventChannelSO>();
            var targetScoreReached = ScriptableObject.CreateInstance<PlayerScoreEventChannelSO>();
            var objectiveCompleted = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            var playerID = ScriptableObject.CreateInstance<PlayerIDSO>();

            // Set private serialized fields via reflection
            SetPrivateField(scoreObjective, "m_TargetScore", 3);
            SetPrivateField(scoreObjective, "m_ScoreManagerUpdated", scoreManagerUpdated);
            SetPrivateField(scoreObjective, "m_TargetScoreReached", targetScoreReached);
            SetPrivateField(scoreObjective, "m_ObjectiveCompleted", objectiveCompleted);

            // Re-trigger OnEnable now that fields are assigned
            InvokePrivateMethod(scoreObjective, "OnEnable");

            // Subscribe to the target score reached event
            bool targetReached = false;
            PlayerScore winningPlayer = default;
            targetScoreReached.OnEventRaised += (ps) =>
            {
                targetReached = true;
                winningPlayer = ps;
            };

            // Build a player score that meets the target
            var score = new Score();
            score.IncrementScore();
            score.IncrementScore();
            score.IncrementScore();

            var playerScores = new List<PlayerScore>
            {
                new PlayerScore { playerID = playerID, score = score }
            };

            // Simulate ScoreManager broadcasting updated scores
            scoreManagerUpdated.RaiseEvent(playerScores);

            Assert.IsTrue(targetReached, "TargetScoreReached event should fire when a player reaches the target score");
            Assert.AreEqual(playerID, winningPlayer.playerID);
            Assert.AreEqual(3, winningPlayer.score.Value);

            Object.DestroyImmediate(scoreObjective);
            Object.DestroyImmediate(scoreManagerUpdated);
            Object.DestroyImmediate(targetScoreReached);
            Object.DestroyImmediate(objectiveCompleted);
            Object.DestroyImmediate(playerID);
        }

        [Test]
        public void ScoreObjective_PlayerBelowTarget_DoesNotRaiseEvent()
        {
            var scoreObjective = ScriptableObject.CreateInstance<ScoreObjectiveSO>();
            var scoreManagerUpdated = ScriptableObject.CreateInstance<ScoreListEventChannelSO>();
            var targetScoreReached = ScriptableObject.CreateInstance<PlayerScoreEventChannelSO>();
            var objectiveCompleted = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            var playerID = ScriptableObject.CreateInstance<PlayerIDSO>();

            SetPrivateField(scoreObjective, "m_TargetScore", 5);
            SetPrivateField(scoreObjective, "m_ScoreManagerUpdated", scoreManagerUpdated);
            SetPrivateField(scoreObjective, "m_TargetScoreReached", targetScoreReached);
            SetPrivateField(scoreObjective, "m_ObjectiveCompleted", objectiveCompleted);

            // Re-trigger OnEnable now that fields are assigned
            InvokePrivateMethod(scoreObjective, "OnEnable");

            bool targetReached = false;
            targetScoreReached.OnEventRaised += (ps) => targetReached = true;

            // Player has only 2 points, target is 5
            var score = new Score();
            score.IncrementScore();
            score.IncrementScore();

            var playerScores = new List<PlayerScore>
            {
                new PlayerScore { playerID = playerID, score = score }
            };

            scoreManagerUpdated.RaiseEvent(playerScores);

            Assert.IsFalse(targetReached, "TargetScoreReached should not fire when score is below target");

            Object.DestroyImmediate(scoreObjective);
            Object.DestroyImmediate(scoreManagerUpdated);
            Object.DestroyImmediate(targetScoreReached);
            Object.DestroyImmediate(objectiveCompleted);
            Object.DestroyImmediate(playerID);
        }

        [Test]
        public void ObjectiveSO_CompleteObjective_RaisesCompletedEvent()
        {
            var objective = ScriptableObject.CreateInstance<ObjectiveSO>();
            var completedChannel = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            SetPrivateField(objective, "m_ObjectiveCompleted", completedChannel);

            bool eventFired = false;
            completedChannel.OnEventRaised += () => eventFired = true;

            InvokePrivateMethod(objective, "CompleteObjective");

            Assert.IsTrue(eventFired, "ObjectiveCompleted event should fire when objective is completed");
            Assert.IsTrue(objective.IsCompleted);

            Object.DestroyImmediate(objective);
            Object.DestroyImmediate(completedChannel);
        }

        [Test]
        public void ObjectiveSO_ResetObjective_ClearsCompletedState()
        {
            var objective = ScriptableObject.CreateInstance<ObjectiveSO>();
            var completedChannel = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            SetPrivateField(objective, "m_ObjectiveCompleted", completedChannel);

            InvokePrivateMethod(objective, "CompleteObjective");
            Assert.IsTrue(objective.IsCompleted);

            objective.ResetObjective();

            Assert.IsFalse(objective.IsCompleted);

            Object.DestroyImmediate(objective);
            Object.DestroyImmediate(completedChannel);
        }

        // ── Helpers ──

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            Assert.Fail($"Field '{fieldName}' not found on {target.GetType().Name}");
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var type = target.GetType();
            while (type != null)
            {
                var method = type.GetMethod(methodName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(target, args.Length > 0 ? args : null);
                    return;
                }
                type = type.BaseType;
            }
            Assert.Fail($"Method '{methodName}' not found on {target.GetType().Name}");
        }
    }
}
