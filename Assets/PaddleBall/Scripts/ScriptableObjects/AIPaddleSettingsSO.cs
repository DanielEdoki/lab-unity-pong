using UnityEngine;

namespace GameSystemsCookbook.Demos.PaddleBall
{
    public enum AIDifficulty
    {
        Custom,
        Bad,
        Casual,
        Pro
    }

    /// <summary>
    /// Snapshot of AI settings used at runtime. Avoids modifying the ScriptableObject asset.
    /// </summary>
    public struct AIPaddleConfig
    {
        public float ReactionTime;
        public float TrackingSpeedMultiplier;
        public float AccuracyOffset;
        public float DeadZone;
        public float ResponseSharpness;
        public bool PredictionEnabled;
        public float PlayAreaMinY;
        public float PlayAreaMaxY;
    }

    /// <summary>
    /// Configuration for the AI-controlled paddle. Adjust these values to tune difficulty.
    /// </summary>
    [CreateAssetMenu(menuName = "PaddleBall/AI Paddle Settings", fileName = "AIPaddleSettings")]
    public class AIPaddleSettingsSO : DescriptionSO
    {
        /// <summary>
        /// Runtime-only difficulty selection set by the UI. Resets on domain reload.
        /// When set to Custom, the SO's serialized values are used.
        /// </summary>
        public static AIDifficulty ActiveDifficulty { get; set; } = AIDifficulty.Custom;

        public static AIPaddleConfig GetPreset(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Bad:
                    return new AIPaddleConfig
                    {
                        ReactionTime = 0.4f,
                        TrackingSpeedMultiplier = 0.5f,
                        AccuracyOffset = 1.5f,
                        DeadZone = 0.5f,
                        ResponseSharpness = 0.5f,
                        PredictionEnabled = false,
                        PlayAreaMinY = -4.5f,
                        PlayAreaMaxY = 4.5f
                    };
                case AIDifficulty.Casual:
                    return new AIPaddleConfig
                    {
                        ReactionTime = 0.15f,
                        TrackingSpeedMultiplier = 0.85f,
                        AccuracyOffset = 0.3f,
                        DeadZone = 0.2f,
                        ResponseSharpness = 1.5f,
                        PredictionEnabled = true,
                        PlayAreaMinY = -4.5f,
                        PlayAreaMaxY = 4.5f
                    };
                case AIDifficulty.Pro:
                    return new AIPaddleConfig
                    {
                        ReactionTime = 0f,
                        TrackingSpeedMultiplier = 3f,
                        AccuracyOffset = 0f,
                        DeadZone = 0f,
                        ResponseSharpness = 5f,
                        PredictionEnabled = true,
                        PlayAreaMinY = -4.5f,
                        PlayAreaMaxY = 4.5f
                    };
                default:
                    return default;
            }
        }

        public AIPaddleConfig GetActiveConfig()
        {
            if (ActiveDifficulty != AIDifficulty.Custom)
                return GetPreset(ActiveDifficulty);

            return new AIPaddleConfig
            {
                ReactionTime = m_ReactionTime,
                TrackingSpeedMultiplier = m_TrackingSpeedMultiplier,
                AccuracyOffset = m_AccuracyOffset,
                DeadZone = m_DeadZone,
                ResponseSharpness = m_ResponseSharpness,
                PredictionEnabled = m_PredictionEnabled,
                PlayAreaMinY = m_PlayAreaMinY,
                PlayAreaMaxY = m_PlayAreaMaxY
            };
        }

        [Header("AI Toggle")]
        [Tooltip("Enable AI control for Player 2")]
        [SerializeField] private bool m_AIEnabled = true;

        [Header("Difficulty")]
        [Tooltip("Seconds before AI reacts to ball position changes (0 = every frame)")]
        [Range(0f, 0.6f)]
        [SerializeField] private float m_ReactionTime = 0.2f;

        [Tooltip("Force multiplier relative to paddle speed (higher = faster paddle)")]
        [Range(0.3f, 5f)]
        [SerializeField] private float m_TrackingSpeedMultiplier = 0.75f;

        [Tooltip("Random vertical offset added to target position (0 = perfect aim)")]
        [Range(0f, 2f)]
        [SerializeField] private float m_AccuracyOffset = 0.5f;

        [Tooltip("Vertical distance within which the AI won't move (prevents jitter)")]
        [Range(0f, 1f)]
        [SerializeField] private float m_DeadZone = 0.3f;

        [Tooltip("How aggressively the AI reaches full speed (higher = snappier response)")]
        [Range(0.2f, 5f)]
        [SerializeField] private float m_ResponseSharpness = 1f;

        [Header("Prediction")]
        [Tooltip("AI predicts where the ball will arrive at the paddle's X position, accounting for wall bounces")]
        [SerializeField] private bool m_PredictionEnabled;

        [Header("Play Area (for trajectory prediction)")]
        [Tooltip("Bottom wall Y position for bounce calculation")]
        [SerializeField] private float m_PlayAreaMinY = -4.5f;

        [Tooltip("Top wall Y position for bounce calculation")]
        [SerializeField] private float m_PlayAreaMaxY = 4.5f;

        public bool Enabled => m_AIEnabled;
        public float ReactionTime => m_ReactionTime;
        public float TrackingSpeedMultiplier => m_TrackingSpeedMultiplier;
        public float AccuracyOffset => m_AccuracyOffset;
        public float DeadZone => m_DeadZone;
        public float ResponseSharpness => m_ResponseSharpness;
        public bool PredictionEnabled => m_PredictionEnabled;
        public float PlayAreaMinY => m_PlayAreaMinY;
        public float PlayAreaMaxY => m_PlayAreaMaxY;
    }
}
