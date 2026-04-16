using UnityEngine;

namespace GameSystemsCookbook.Demos.PaddleBall
{
    /// <summary>
    /// AI controller that drives a Paddle by tracking the ball position.
    /// Uses proportional input, trajectory prediction with wall bounces, and configurable imperfections.
    /// Reads difficulty presets from AIPaddleSettingsSO.ActiveDifficulty at initialization.
    /// </summary>
    public class AIPaddle : MonoBehaviour
    {
        private Paddle m_Paddle;
        private Transform m_BallTransform;
        private Rigidbody2D m_BallRigidbody;
        private AIPaddleConfig m_Config;

        private float m_ReactionTimer;
        private float m_TargetY;
        private float m_CurrentOffset;
        private float m_PaddleX;

        public void Initialize(AIPaddleSettingsSO settings, Transform ballTransform, Rigidbody2D ballRigidbody)
        {
            m_Config = settings.GetActiveConfig();
            m_BallTransform = ballTransform;
            m_BallRigidbody = ballRigidbody;
            m_Paddle = GetComponent<Paddle>();

            m_ReactionTimer = 0f;
            m_TargetY = transform.position.y;
            m_CurrentOffset = 0f;
            m_PaddleX = transform.position.x;

            Debug.Log($"[AIPaddle] Initialized with difficulty: {AIPaddleSettingsSO.ActiveDifficulty} " +
                      $"(reaction={m_Config.ReactionTime}, speed={m_Config.TrackingSpeedMultiplier}, " +
                      $"prediction={m_Config.PredictionEnabled})");
        }

        private void Update()
        {
            if (m_BallTransform == null || m_Paddle == null)
                return;

            UpdateTarget();
            ApplyInput();
        }

        private void UpdateTarget()
        {
            // Zero reaction time = update every frame (perfect tracking)
            if (m_Config.ReactionTime <= 0f)
            {
                m_TargetY = ComputeTargetY();
                m_CurrentOffset = (m_Config.AccuracyOffset > 0f)
                    ? Random.Range(-m_Config.AccuracyOffset, m_Config.AccuracyOffset)
                    : 0f;
                return;
            }

            m_ReactionTimer += Time.deltaTime;

            if (m_ReactionTimer >= m_Config.ReactionTime)
            {
                m_ReactionTimer = 0f;
                m_TargetY = ComputeTargetY();
                m_CurrentOffset = Random.Range(-m_Config.AccuracyOffset, m_Config.AccuracyOffset);
            }
        }

        private float ComputeTargetY()
        {
            if (m_BallRigidbody == null)
                return m_BallTransform.position.y;

            Vector2 ballVelocity = m_BallRigidbody.linearVelocity;

            if (m_Config.PredictionEnabled)
                return PredictArrivalY(ballVelocity);

            return m_BallTransform.position.y;
        }

        private float PredictArrivalY(Vector2 ballVelocity)
        {
            float ballX = m_BallTransform.position.x;
            float ballY = m_BallTransform.position.y;

            // Ball nearly stationary horizontally — hold current position
            if (Mathf.Abs(ballVelocity.x) < 0.1f)
                return transform.position.y;

            bool ballMovingTowardPaddle = (m_PaddleX > 0 && ballVelocity.x > 0) ||
                                          (m_PaddleX < 0 && ballVelocity.x < 0);

            // Ball moving away — hold current position instead of drifting to center
            if (!ballMovingTowardPaddle)
                return transform.position.y;

            // Time for ball to reach the paddle's X position
            float timeToArrive = (m_PaddleX - ballX) / ballVelocity.x;

            if (timeToArrive <= 0f)
                return ballY;

            // Predict Y with wall bounces using zigzag fold
            float rawY = ballY + ballVelocity.y * timeToArrive;
            return FoldBounceY(rawY, m_Config.PlayAreaMinY, m_Config.PlayAreaMaxY);
        }

        /// <summary>
        /// Simulates wall bounces by folding the Y position within play area bounds.
        /// Works like a zigzag wave — maps any Y value back into [minY, maxY].
        /// </summary>
        public static float FoldBounceY(float y, float minY, float maxY)
        {
            float range = maxY - minY;

            if (range <= 0f)
                return minY;

            float relative = y - minY;
            float normalized = relative / range;

            // Modulo into [0, 2) for one full bounce cycle
            float mod = normalized % 2f;
            if (mod < 0f)
                mod += 2f;

            if (mod <= 1f)
                return minY + mod * range;

            return maxY - (mod - 1f) * range;
        }

        private void ApplyInput()
        {
            float adjustedTarget = m_TargetY + m_CurrentOffset;
            float difference = adjustedTarget - transform.position.y;

            if (Mathf.Abs(difference) < m_Config.DeadZone)
            {
                m_Paddle.SetAIInput(Vector2.zero);
                return;
            }

            // Proportional input scaled by response sharpness
            // Higher sharpness = reaches full speed at smaller distances
            float proportional = Mathf.Clamp(difference * m_Config.ResponseSharpness, -1f, 1f);
            Vector2 input = new Vector2(0f, proportional * m_Config.TrackingSpeedMultiplier);
            m_Paddle.SetAIInput(input);
        }
    }
}
