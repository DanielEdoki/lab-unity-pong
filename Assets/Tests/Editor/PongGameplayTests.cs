using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using GameSystemsCookbook;
using GameSystemsCookbook.Demos.PaddleBall;

namespace GameSystemsCookbook.Tests
{
    /// <summary>
    /// EditMode tests covering event channels, GameDataSO, LevelLayoutSO, and GameManager event wiring.
    /// </summary>
    public class PongGameplayTests
    {
        // ── Additional Event Channel Tests ──

        [Test]
        public void StringEventChannel_RaiseEvent_PassesCorrectPayload()
        {
            var channel = ScriptableObject.CreateInstance<StringEventChannelSO>();
            string received = null;
            channel.OnEventRaised += (value) => received = value;

            channel.RaiseEvent("Player 1 wins");

            Assert.AreEqual("Player 1 wins", received);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void BoolEventChannel_RaiseEvent_PassesCorrectPayload()
        {
            var channel = ScriptableObject.CreateInstance<BoolEventChannelSO>();
            bool received = false;
            channel.OnEventRaised += (value) => received = value;

            channel.RaiseEvent(true);

            Assert.IsTrue(received);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void PlayerIDEventChannel_RaiseEvent_PassesCorrectPayload()
        {
            var channel = ScriptableObject.CreateInstance<PlayerIDEventChannelSO>();
            var playerID = ScriptableObject.CreateInstance<PlayerIDSO>();
            PlayerIDSO received = null;
            channel.OnEventRaised += (value) => received = value;

            channel.RaiseEvent(playerID);

            Assert.AreEqual(playerID, received);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(playerID);
        }

        [Test]
        public void Vector2EventChannel_RaiseEvent_PassesCorrectPayload()
        {
            var channel = ScriptableObject.CreateInstance<Vector2EventChannelSO>();
            Vector2 received = Vector2.zero;
            channel.OnEventRaised += (value) => received = value;

            var expected = new Vector2(1.5f, -0.7f);
            channel.RaiseEvent(expected);

            Assert.AreEqual(expected, received);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void GenericEventChannel_RaiseEvent_WithNoSubscribers_DoesNotThrow()
        {
            var channel = ScriptableObject.CreateInstance<StringEventChannelSO>();

            Assert.DoesNotThrow(() => channel.RaiseEvent("test"));
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void GenericEventChannel_MultipleSubscribers_AllNotified()
        {
            var channel = ScriptableObject.CreateInstance<IntEventChannelSO>();
            int count = 0;
            channel.OnEventRaised += (_) => count++;
            channel.OnEventRaised += (_) => count++;
            channel.OnEventRaised += (_) => count++;

            channel.RaiseEvent(1);

            Assert.AreEqual(3, count);
            Object.DestroyImmediate(channel);
        }

        // ── GameDataSO Tests ──

        [Test]
        public void GameDataSO_IsPlayer1_ReturnsTrueForPlayer1()
        {
            var gameData = ScriptableObject.CreateInstance<GameDataSO>();
            var player1 = ScriptableObject.CreateInstance<PlayerIDSO>();
            var player2 = ScriptableObject.CreateInstance<PlayerIDSO>();

            SetPrivateField(gameData, "m_Player1", player1);
            SetPrivateField(gameData, "m_Player2", player2);

            Assert.IsTrue(gameData.IsPlayer1(player1));
            Assert.IsFalse(gameData.IsPlayer1(player2));

            Object.DestroyImmediate(gameData);
            Object.DestroyImmediate(player1);
            Object.DestroyImmediate(player2);
        }

        [Test]
        public void GameDataSO_IsPlayer2_ReturnsTrueForPlayer2()
        {
            var gameData = ScriptableObject.CreateInstance<GameDataSO>();
            var player1 = ScriptableObject.CreateInstance<PlayerIDSO>();
            var player2 = ScriptableObject.CreateInstance<PlayerIDSO>();

            SetPrivateField(gameData, "m_Player1", player1);
            SetPrivateField(gameData, "m_Player2", player2);

            Assert.IsTrue(gameData.IsPlayer2(player2));
            Assert.IsFalse(gameData.IsPlayer2(player1));

            Object.DestroyImmediate(gameData);
            Object.DestroyImmediate(player1);
            Object.DestroyImmediate(player2);
        }

        [Test]
        public void GameDataSO_Properties_ReturnExpectedDefaults()
        {
            var gameData = ScriptableObject.CreateInstance<GameDataSO>();

            Assert.AreEqual(80f, gameData.PaddleSpeed);
            Assert.AreEqual(10f, gameData.PaddleLinearDrag);
            Assert.AreEqual(0.5f, gameData.PaddleMass);
            Assert.AreEqual(200f, gameData.BallSpeed);
            Assert.AreEqual(300f, gameData.MaxSpeed);
            Assert.AreEqual(1.1f, gameData.BounceMultiplier);
            Assert.AreEqual(1f, gameData.DelayBetweenPoints);

            Object.DestroyImmediate(gameData);
        }

        // ── LevelLayoutSO Tests ──

        [Test]
        public void LevelLayoutSO_Properties_ReturnConfiguredPositions()
        {
            var layout = ScriptableObject.CreateInstance<LevelLayoutSO>();
            var ballPos = new Vector3(0f, 0f, 0f);
            var paddle1Pos = new Vector3(-5f, 0f, 0f);
            var paddle2Pos = new Vector3(5f, 0f, 0f);

            SetPrivateField(layout, "m_BallStartPosition", ballPos);
            SetPrivateField(layout, "m_Paddle1StartPosition", paddle1Pos);
            SetPrivateField(layout, "m_Paddle2StartPosition", paddle2Pos);

            Assert.AreEqual(ballPos, layout.BallStartPosition);
            Assert.AreEqual(paddle1Pos, layout.Paddle1StartPosition);
            Assert.AreEqual(paddle2Pos, layout.Paddle2StartPosition);

            Object.DestroyImmediate(layout);
        }

        [Test]
        public void LevelLayoutSO_OnValidate_SetsDefaultJsonFilename()
        {
            var layout = ScriptableObject.CreateInstance<LevelLayoutSO>();
            SetPrivateField(layout, "m_JsonFilename", "");

            InvokePrivateMethod(layout, "OnValidate");

            Assert.AreEqual("LevelLayout.json", layout.JsonFilename);
            Object.DestroyImmediate(layout);
        }

        [Test]
        public void LevelLayoutSO_GoalProperties_ReturnConfiguredData()
        {
            var layout = ScriptableObject.CreateInstance<LevelLayoutSO>();
            var goal1 = new TransformSaveData
            {
                position = new Vector3(-7f, 0f, 0f),
                rotation = Vector3.zero,
                localScale = new Vector3(1f, 5f, 1f)
            };

            SetPrivateField(layout, "m_Goal1", goal1);

            Assert.AreEqual(goal1.position, layout.Goal1.position);
            Assert.AreEqual(goal1.localScale, layout.Goal1.localScale);

            Object.DestroyImmediate(layout);
        }

        // ── GameManager Event Wiring Tests ──

        [Test]
        public void GameManager_GoalHit_RelaysToPointScored()
        {
            var go = CreateGameManagerObject(out var goalHit, out var pointScored,
                out _, out _, out _, out _, out _, out _, out _, out _);

            var playerID = ScriptableObject.CreateInstance<PlayerIDSO>();
            PlayerIDSO received = null;
            pointScored.OnEventRaised += (id) => received = id;

            goalHit.RaiseEvent(playerID);

            Assert.AreEqual(playerID, received);

            Object.DestroyImmediate(playerID);
            CleanupGameManager(go);
        }

        [Test]
        public void GameManager_TargetScoreReached_BroadcastsWinnerMessage()
        {
            var go = CreateGameManagerObject(out _, out _, out var scoreTargetReached,
                out var winnerShown, out _, out _, out _, out _, out _, out _);

            var playerID = ScriptableObject.CreateInstance<PlayerIDSO>();
            playerID.name = "Player1_SO";
            var score = new Score();
            score.IncrementScore();
            score.IncrementScore();
            score.IncrementScore();
            var playerScore = new PlayerScore { playerID = playerID, score = score };

            string receivedMessage = null;
            winnerShown.OnEventRaised += (msg) => receivedMessage = msg;

            scoreTargetReached.RaiseEvent(playerScore);

            Assert.AreEqual("Player1 wins", receivedMessage);

            Object.DestroyImmediate(playerID);
            CleanupGameManager(go);
        }

        [Test]
        public void GameManager_EndGame_SetsGameOverFlag()
        {
            var go = CreateGameManagerObject(out _, out _, out _, out _,
                out _, out _, out _, out _, out _, out _);
            var gm = go.GetComponent<GameManager>();

            gm.EndGame();

            bool isGameOver = (bool)GetPrivateField(gm, "m_IsGameOver");
            Assert.IsTrue(isGameOver);

            CleanupGameManager(go);
        }

        [Test]
        public void GameManager_ResetGame_ClearsGameOverAndStartsGame()
        {
            var go = CreateGameManagerObject(out _, out _, out _, out _,
                out _, out _, out var gameStarted, out _, out var gameReset, out _);
            var gm = go.GetComponent<GameManager>();

            gm.EndGame();
            bool isGameOverBefore = (bool)GetPrivateField(gm, "m_IsGameOver");
            Assert.IsTrue(isGameOverBefore);

            bool gameStartedFired = false;
            gameStarted.OnEventRaised += () => gameStartedFired = true;

            gameReset.RaiseEvent();

            bool isGameOverAfter = (bool)GetPrivateField(gm, "m_IsGameOver");
            Assert.IsFalse(isGameOverAfter);
            Assert.IsTrue(gameStartedFired);

            CleanupGameManager(go);
        }

        [Test]
        public void GameManager_OnReplay_IgnoredIfGameNotOver()
        {
            var go = CreateGameManagerObject(out _, out _, out _, out _,
                out _, out _, out var gameStarted, out _, out _, out _);
            var gm = go.GetComponent<GameManager>();
            var inputReader = (InputReaderSO)GetPrivateField(gm, "m_InputReader");

            int startCount = 0;
            gameStarted.OnEventRaised += () => startCount++;

            // Game is not over, so replay should be ignored
            inputReader.GetType()
                .GetField("GameRestarted", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(inputReader);

            // Invoke OnReplay directly
            InvokePrivateMethod(gm, "OnReplay");

            Assert.AreEqual(0, startCount, "OnReplay should be ignored when game is not over");

            CleanupGameManager(go);
        }

        [Test]
        public void GameManager_OnReplay_RestartsGameWhenGameOver()
        {
            var go = CreateGameManagerObject(out _, out _, out _, out _,
                out _, out _, out var gameStarted, out _, out _, out _);
            var gm = go.GetComponent<GameManager>();

            gm.EndGame();

            int startCount = 0;
            gameStarted.OnEventRaised += () => startCount++;

            InvokePrivateMethod(gm, "OnReplay");

            Assert.AreEqual(1, startCount, "OnReplay should start game when game is over");

            bool isGameOver = (bool)GetPrivateField(gm, "m_IsGameOver");
            Assert.IsFalse(isGameOver, "Game over flag should be cleared after replay");

            CleanupGameManager(go);
        }

        // ── Helpers ──

        private static GameObject CreateGameManagerObject(
            out PlayerIDEventChannelSO goalHit,
            out PlayerIDEventChannelSO pointScored,
            out PlayerScoreEventChannelSO scoreTargetReached,
            out StringEventChannelSO winnerShown,
            out VoidEventChannelSO gameEnded,
            out VoidEventChannelSO allObjectivesCompleted,
            out VoidEventChannelSO gameStarted,
            out VoidEventChannelSO gameQuit,
            out VoidEventChannelSO gameReset,
            out BoolEventChannelSO isPaused)
        {
            var go = new GameObject("TestGameManager");
            go.AddComponent<GameSetup>();
            var gm = go.AddComponent<GameManager>();

            // Create all required event channels
            goalHit = ScriptableObject.CreateInstance<PlayerIDEventChannelSO>();
            pointScored = ScriptableObject.CreateInstance<PlayerIDEventChannelSO>();
            scoreTargetReached = ScriptableObject.CreateInstance<PlayerScoreEventChannelSO>();
            winnerShown = ScriptableObject.CreateInstance<StringEventChannelSO>();
            gameEnded = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            allObjectivesCompleted = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            gameStarted = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            gameQuit = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            gameReset = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            isPaused = ScriptableObject.CreateInstance<BoolEventChannelSO>();
            var sceneUnloaded = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            var homeScreenShown = ScriptableObject.CreateInstance<VoidEventChannelSO>();
            var inputReader = ScriptableObject.CreateInstance<InputReaderSO>();

            // Wire up all serialized fields
            SetPrivateField(gm, "m_AutoStart", false);
            SetPrivateField(gm, "m_GameSetup", go.GetComponent<GameSetup>());
            SetPrivateField(gm, "m_InputReader", inputReader);
            SetPrivateField(gm, "m_GameStarted", gameStarted);
            SetPrivateField(gm, "m_GameEnded", gameEnded);
            SetPrivateField(gm, "m_PointScored", pointScored);
            SetPrivateField(gm, "m_WinnerShown", winnerShown);
            SetPrivateField(gm, "m_SceneUnloaded", sceneUnloaded);
            SetPrivateField(gm, "m_HomeScreenShown", homeScreenShown);
            SetPrivateField(gm, "m_GoalHit", goalHit);
            SetPrivateField(gm, "m_ScoreTargetReached", scoreTargetReached);
            SetPrivateField(gm, "m_AllObjectivesCompleted", allObjectivesCompleted);
            SetPrivateField(gm, "m_GameReset", gameReset);
            SetPrivateField(gm, "m_IsPaused", isPaused);
            SetPrivateField(gm, "m_GameQuit", gameQuit);

            // Manually trigger OnEnable to subscribe to events
            InvokePrivateMethod(gm, "OnEnable");

            return go;
        }

        private static void CleanupGameManager(GameObject go)
        {
            var gm = go.GetComponent<GameManager>();
            InvokePrivateMethod(gm, "OnDisable");
            Object.DestroyImmediate(go);
        }

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

        private static object GetPrivateField(object target, string fieldName)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    return field.GetValue(target);
                type = type.BaseType;
            }
            Assert.Fail($"Field '{fieldName}' not found on {target.GetType().Name}");
            return null;
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
