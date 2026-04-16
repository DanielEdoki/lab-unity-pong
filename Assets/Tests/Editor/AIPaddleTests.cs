using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using GameSystemsCookbook;
using GameSystemsCookbook.Demos.PaddleBall;

namespace GameSystemsCookbook.Tests
{
    /// <summary>
    /// EditMode tests for AI paddle settings, trajectory prediction, and integration.
    /// </summary>
    public class AIPaddleTests
    {
        // ── AIPaddleSettingsSO Tests ──

        [Test]
        public void AIPaddleSettings_DefaultValues_AreReasonable()
        {
            var settings = ScriptableObject.CreateInstance<AIPaddleSettingsSO>();

            Assert.IsTrue(settings.Enabled);
            Assert.AreEqual(0.2f, settings.ReactionTime);
            Assert.AreEqual(0.75f, settings.TrackingSpeedMultiplier);
            Assert.AreEqual(0.5f, settings.AccuracyOffset);
            Assert.AreEqual(0.3f, settings.DeadZone);
            Assert.AreEqual(1f, settings.ResponseSharpness);
            Assert.IsFalse(settings.PredictionEnabled);
            Assert.AreEqual(-4.5f, settings.PlayAreaMinY);
            Assert.AreEqual(4.5f, settings.PlayAreaMaxY);

            Object.DestroyImmediate(settings);
        }

        [Test]
        public void AIPaddleSettings_ReactionTime_IsWithinPlayableRange()
        {
            var settings = ScriptableObject.CreateInstance<AIPaddleSettingsSO>();

            Assert.GreaterOrEqual(settings.ReactionTime, 0f,
                "Reaction time minimum should allow every-frame response");
            Assert.LessOrEqual(settings.ReactionTime, 0.6f,
                "Reaction time should not make AI unresponsive");

            Object.DestroyImmediate(settings);
        }

        [Test]
        public void AIPaddleSettings_TrackingSpeed_IsSubMaximum()
        {
            var settings = ScriptableObject.CreateInstance<AIPaddleSettingsSO>();

            Assert.Less(settings.TrackingSpeedMultiplier, 1.0f,
                "Default AI should be slower than max paddle speed");
            Assert.Greater(settings.TrackingSpeedMultiplier, 0f,
                "AI must have some tracking speed");

            Object.DestroyImmediate(settings);
        }

        // ── FoldBounceY Trajectory Prediction Tests ──

        [Test]
        public void FoldBounceY_WithinBounds_ReturnsUnchanged()
        {
            float result = AIPaddle.FoldBounceY(2f, -4.5f, 4.5f);

            Assert.AreEqual(2f, result, 0.001f);
        }

        [Test]
        public void FoldBounceY_AtMinBound_ReturnsMin()
        {
            float result = AIPaddle.FoldBounceY(-4.5f, -4.5f, 4.5f);

            Assert.AreEqual(-4.5f, result, 0.001f);
        }

        [Test]
        public void FoldBounceY_AtMaxBound_ReturnsMax()
        {
            float result = AIPaddle.FoldBounceY(4.5f, -4.5f, 4.5f);

            Assert.AreEqual(4.5f, result, 0.001f);
        }

        [Test]
        public void FoldBounceY_AboveMax_BouncesBack()
        {
            // 6.5 is 2 units above max (4.5), should bounce to 4.5 - 2 = 2.5
            float result = AIPaddle.FoldBounceY(6.5f, -4.5f, 4.5f);

            Assert.AreEqual(2.5f, result, 0.001f);
        }

        [Test]
        public void FoldBounceY_BelowMin_BouncesBack()
        {
            // -6.5 is 2 units below min (-4.5), should bounce to -4.5 + 2 = -2.5
            float result = AIPaddle.FoldBounceY(-6.5f, -4.5f, 4.5f);

            Assert.AreEqual(-2.5f, result, 0.001f);
        }

        [Test]
        public void FoldBounceY_MultipleBounces_FoldsCorrectly()
        {
            // Ball at y=0, travels +18 units (2 full ranges of 9). Should end up at 0 relative = minY + 0 = -4.5
            // Actually: rawY = 0 + 18 = 18. relative = 18 - (-4.5) = 22.5. normalized = 22.5/9 = 2.5
            // mod = 2.5 % 2 = 0.5. Since mod <= 1: result = -4.5 + 0.5*9 = 0.0
            float result = AIPaddle.FoldBounceY(18f, -4.5f, 4.5f);

            Assert.AreEqual(0f, result, 0.001f);
        }

        [Test]
        public void FoldBounceY_NegativeMultipleBounces_FoldsCorrectly()
        {
            // rawY = -18. relative = -18 - (-4.5) = -13.5. normalized = -13.5/9 = -1.5
            // mod = -1.5 % 2 = -1.5 + 2 = 0.5. Since mod <= 1: result = -4.5 + 0.5*9 = 0
            float result = AIPaddle.FoldBounceY(-18f, -4.5f, 4.5f);

            Assert.AreEqual(0f, result, 0.001f);
        }

        // ── AIPaddle Component Tests ──

        [Test]
        public void AIPaddle_Initialize_StoresReferences()
        {
            var paddleGO = new GameObject("TestPaddle");
            var rb = paddleGO.AddComponent<Rigidbody2D>();
            var paddle = paddleGO.AddComponent<Paddle>();
            var aiPaddle = paddleGO.AddComponent<AIPaddle>();

            var settings = ScriptableObject.CreateInstance<AIPaddleSettingsSO>();
            var ballGO = new GameObject("TestBall");
            var ballRb = ballGO.AddComponent<Rigidbody2D>();

            aiPaddle.Initialize(settings, ballGO.transform, ballRb);

            var ballTransformField = GetPrivateField(aiPaddle, "m_BallTransform");
            var ballRbField = GetPrivateField(aiPaddle, "m_BallRigidbody");

            Assert.AreEqual(ballGO.transform, ballTransformField);
            Assert.AreEqual(ballRb, ballRbField);

            Object.DestroyImmediate(paddleGO);
            Object.DestroyImmediate(ballGO);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void AIPaddle_Initialize_CachesPaddleXPosition()
        {
            var paddleGO = new GameObject("TestPaddle");
            paddleGO.transform.position = new Vector3(5f, 0f, 0f);
            paddleGO.AddComponent<Rigidbody2D>();
            paddleGO.AddComponent<Paddle>();
            var aiPaddle = paddleGO.AddComponent<AIPaddle>();

            var settings = ScriptableObject.CreateInstance<AIPaddleSettingsSO>();
            var ballGO = new GameObject("TestBall");
            var ballRb = ballGO.AddComponent<Rigidbody2D>();

            aiPaddle.Initialize(settings, ballGO.transform, ballRb);

            var paddleX = (float)GetPrivateField(aiPaddle, "m_PaddleX");
            Assert.AreEqual(5f, paddleX, 0.001f);

            Object.DestroyImmediate(paddleGO);
            Object.DestroyImmediate(ballGO);
            Object.DestroyImmediate(settings);
        }

        // ── Preset / Difficulty Tests ──

        [Test]
        public void AIPaddleSettings_GetPreset_BadIsBeatable()
        {
            var config = AIPaddleSettingsSO.GetPreset(AIDifficulty.Bad);

            Assert.Greater(config.ReactionTime, 0.2f);
            Assert.Less(config.TrackingSpeedMultiplier, 0.7f);
            Assert.Greater(config.AccuracyOffset, 1f);
            Assert.IsFalse(config.PredictionEnabled);
        }

        [Test]
        public void AIPaddleSettings_GetPreset_ProIsMaximal()
        {
            var config = AIPaddleSettingsSO.GetPreset(AIDifficulty.Pro);

            Assert.AreEqual(0f, config.ReactionTime);
            Assert.AreEqual(0f, config.AccuracyOffset);
            Assert.AreEqual(0f, config.DeadZone);
            Assert.IsTrue(config.PredictionEnabled);
            Assert.GreaterOrEqual(config.TrackingSpeedMultiplier, 3f);
        }

        [Test]
        public void AIPaddleSettings_GetActiveConfig_ReturnsPresetWhenSet()
        {
            var settings = ScriptableObject.CreateInstance<AIPaddleSettingsSO>();
            var original = AIPaddleSettingsSO.ActiveDifficulty;

            AIPaddleSettingsSO.ActiveDifficulty = AIDifficulty.Pro;
            var config = settings.GetActiveConfig();

            Assert.AreEqual(0f, config.ReactionTime);
            Assert.IsTrue(config.PredictionEnabled);

            AIPaddleSettingsSO.ActiveDifficulty = original;
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void AIPaddleSettings_GetActiveConfig_ReturnsCustomWhenNoPreset()
        {
            var settings = ScriptableObject.CreateInstance<AIPaddleSettingsSO>();
            var original = AIPaddleSettingsSO.ActiveDifficulty;

            AIPaddleSettingsSO.ActiveDifficulty = AIDifficulty.Custom;
            var config = settings.GetActiveConfig();

            // Should return the SO's default serialized values
            Assert.AreEqual(0.2f, config.ReactionTime);
            Assert.AreEqual(0.75f, config.TrackingSpeedMultiplier);

            AIPaddleSettingsSO.ActiveDifficulty = original;
            Object.DestroyImmediate(settings);
        }

        // ── Paddle Integration Tests ──

        [Test]
        public void Paddle_SetAIInput_OverridesInputVector()
        {
            var paddleGO = new GameObject("TestPaddle");
            paddleGO.AddComponent<Rigidbody2D>();
            var paddle = paddleGO.AddComponent<Paddle>();

            var input = new Vector2(0f, 0.75f);
            paddle.SetAIInput(input);

            var inputVector = GetPrivateField(paddle, "m_inputVector");
            Assert.AreEqual(input, inputVector);

            Object.DestroyImmediate(paddleGO);
        }

        [Test]
        public void Paddle_DisableHumanInput_DoesNotThrowWithNullReader()
        {
            var paddleGO = new GameObject("TestPaddle");
            paddleGO.AddComponent<Rigidbody2D>();
            var paddle = paddleGO.AddComponent<Paddle>();

            Assert.DoesNotThrow(() => paddle.DisableHumanInput());

            Object.DestroyImmediate(paddleGO);
        }

        // ── Helpers ──

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
    }
}
