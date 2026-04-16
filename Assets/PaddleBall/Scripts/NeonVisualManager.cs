using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace GameSystemsCookbook.Demos.PaddleBall
{
    /// <summary>
    /// Manages all neon visual effects: bloom post-processing, emissive materials,
    /// ball trail, impact/goal particles, screen shake, speed-based ball color,
    /// Tron-style background grid, and score bloom pulse.
    /// All effects are generated entirely by code with no external graphic assets.
    /// Auto-discovers event channels at runtime — no Inspector wiring required.
    /// </summary>
    public class NeonVisualManager : MonoBehaviour
    {
        // Neon color palette
        private static readonly Color k_Cyan = new Color(0f, 1f, 1f);
        private static readonly Color k_Blue = new Color(0.2f, 0.4f, 1f);
        private static readonly Color k_Magenta = new Color(1f, 0.1f, 0.8f);
        private static readonly Color k_Purple = new Color(0.5f, 0.15f, 0.8f);
        private static readonly Color k_GridPurple = new Color(0.25f, 0.05f, 0.4f);

        private const float k_NeonIntensity = 2f;
        private const float k_WallIntensity = 1.8f;
        private const float k_GridIntensity = 1.5f;

        // Bloom settings
        private const float k_BloomThreshold = 0.5f;
        private const float k_BloomBaseIntensity = 2f;
        private const float k_BloomScatter = 0.75f;
        private const float k_BloomPulseIntensity = 4f;
        private const float k_BloomPulseDuration = 0.4f;

        // Screen shake settings
        private const float k_ImpactShakeIntensity = 0.1f;
        private const float k_ImpactShakeDuration = 0.12f;
        private const float k_GoalShakeIntensity = 0.25f;
        private const float k_GoalShakeDuration = 0.3f;

        // Trail settings
        private const float k_TrailTime = 0.2f;
        private const float k_TrailStartWidth = 0.25f;
        private const float k_TrailEndWidth = 0f;

        // Grid settings
        private const int k_GridLines = 16;
        private const float k_GridSpacing = 1.5f;
        private const float k_GridLineWidth = 0.015f;
        private const float k_GridZ = 1f;
        private const int k_GridSortingOrder = -100;

        // Impact particle settings
        private const int k_ImpactParticleCount = 12;
        private const float k_ImpactParticleSpeed = 4f;
        private const float k_ImpactParticleLifetime = 0.25f;
        private const float k_ImpactParticleSize = 0.08f;

        // Goal particle settings
        private const int k_GoalParticleCount = 50;
        private const float k_GoalParticleSpeed = 6f;
        private const float k_GoalParticleLifetime = 0.8f;
        private const float k_GoalParticleSize = 0.15f;

        // Auto-discovered event channels
        private Vector2EventChannelSO m_BallCollided;
        private PlayerIDEventChannelSO m_GoalHit;
        private PlayerIDEventChannelSO m_PointScored;

        // Object references
        private Ball m_Ball;
        private Rigidbody2D m_BallRigidbody;
        private SpriteRenderer m_BallSpriteRenderer;
        private Paddle m_Player1;
        private Paddle m_Player2;
        private GameDataSO m_GameData;
        private Camera m_MainCamera;

        // Created components and objects
        private TrailRenderer m_BallTrail;
        private Material m_BallMaterial;
        private Bloom m_Bloom;
        private GameObject m_BloomVolumeGO;
        private ParticleSystem m_ImpactParticles;
        private ParticleSystem m_GoalParticles;
        private GameObject m_GridContainer;
        private Material m_GridMaterial;
        private List<Material> m_CreatedMaterials = new List<Material>();

        // Screen shake state
        private Vector3 m_CameraOriginalPos;
        private float m_ShakeTimer;
        private float m_CurrentShakeDuration;
        private float m_CurrentShakeIntensity;

        // Bloom pulse state
        private float m_BloomPulseTimer;

        // Ball trail sync
        private bool m_BallWasVisible;

        private bool m_Initialized;

        private void OnEnable()
        {
            if (m_Initialized)
                SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            foreach (Material mat in m_CreatedMaterials)
            {
                if (mat != null)
                    Destroy(mat);
            }

            if (m_BloomVolumeGO != null)
                Destroy(m_BloomVolumeGO);
            if (m_ImpactParticles != null)
                Destroy(m_ImpactParticles.gameObject);
            if (m_GoalParticles != null)
                Destroy(m_GoalParticles.gameObject);
            if (m_GridContainer != null)
                Destroy(m_GridContainer);
        }

        public void Initialize(Ball ball, Paddle player1, Paddle player2, GameDataSO gameData)
        {
            m_Ball = ball;
            m_BallRigidbody = ball.GetComponent<Rigidbody2D>();
            m_BallSpriteRenderer = ball.GetComponent<SpriteRenderer>();
            m_Player1 = player1;
            m_Player2 = player2;
            m_GameData = gameData;
            m_MainCamera = Camera.main;

            if (m_MainCamera != null)
                m_CameraOriginalPos = m_MainCamera.transform.position;

            DiscoverEventChannels();
            m_Initialized = true;
            SubscribeToEvents();

            SetupBackground();
            EnableCameraPostProcessing();
            SetupBloom();
            ApplyNeonColors();
            SetupBallTrail();
            CreateImpactParticles();
            CreateGoalParticles();
            CreateTronGrid();
        }

        private void Update()
        {
            if (!m_Initialized)
                return;

            UpdateBallSpeedColor();
            UpdateBallTrailSync();
            UpdateScreenShake();
            UpdateBloomPulse();
            UpdateGridPulse();
        }

        // ==================== EVENT CHANNEL DISCOVERY ====================

        private void DiscoverEventChannels()
        {
            if (m_BallCollided == null)
                m_BallCollided = FindAsset<Vector2EventChannelSO>("BallCollided_SO");
            if (m_GoalHit == null)
                m_GoalHit = FindAsset<PlayerIDEventChannelSO>("GoalHit_SO");
            if (m_PointScored == null)
                m_PointScored = FindAsset<PlayerIDEventChannelSO>("PointScored_SO");
        }

        private T FindAsset<T>(string assetName) where T : ScriptableObject
        {
            T[] assets = Resources.FindObjectsOfTypeAll<T>();
            foreach (T asset in assets)
            {
                if (asset.name == assetName)
                    return asset;
            }
            return null;
        }

        private void SubscribeToEvents()
        {
            if (m_BallCollided != null)
                m_BallCollided.OnEventRaised += OnBallCollided;
            if (m_GoalHit != null)
                m_GoalHit.OnEventRaised += OnGoalHit;
            if (m_PointScored != null)
                m_PointScored.OnEventRaised += OnPointScored;
        }

        private void UnsubscribeFromEvents()
        {
            if (m_BallCollided != null)
                m_BallCollided.OnEventRaised -= OnBallCollided;
            if (m_GoalHit != null)
                m_GoalHit.OnEventRaised -= OnGoalHit;
            if (m_PointScored != null)
                m_PointScored.OnEventRaised -= OnPointScored;
        }

        // ==================== SETUP METHODS ====================

        private void SetupBackground()
        {
            if (m_MainCamera != null)
                m_MainCamera.backgroundColor = new Color(0.02f, 0.01f, 0.05f);
        }

        private void EnableCameraPostProcessing()
        {
            if (m_MainCamera == null)
                return;

            var cameraData = m_MainCamera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null)
                cameraData.renderPostProcessing = true;
        }

        private void SetupBloom()
        {
            m_BloomVolumeGO = new GameObject("NeonBloomVolume");
            Volume volume = m_BloomVolumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

            m_Bloom = profile.Add<Bloom>();
            m_Bloom.active = true;
            m_Bloom.threshold.Override(k_BloomThreshold);
            m_Bloom.intensity.Override(k_BloomBaseIntensity);
            m_Bloom.scatter.Override(k_BloomScatter);

            Vignette vignette = profile.Add<Vignette>();
            vignette.active = true;
            vignette.intensity.Override(0.35f);
            vignette.color.Override(new Color(0.1f, 0f, 0.2f));

            volume.profile = profile;
        }

        private void ApplyNeonColors()
        {
            m_BallMaterial = ApplyNeonToSprites(m_Ball.gameObject, k_Cyan, k_NeonIntensity);
            ApplyNeonToSprites(m_Player1.gameObject, k_Blue, k_NeonIntensity);
            ApplyNeonToSprites(m_Player2.gameObject, k_Magenta, k_NeonIntensity);
            ApplyNeonToWalls();
        }

        private Material ApplyNeonToSprites(GameObject target, Color color, float intensity)
        {
            Material lastMat = null;
            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in renderers)
            {
                Material mat = CreateNeonMaterial(color, intensity);
                sr.material = mat;
                lastMat = mat;
            }
            return lastMat;
        }

        private void ApplyNeonToWalls()
        {
            GameObject levelRoot = GameObject.Find(GameSetup.k_RootTransform);
            if (levelRoot == null)
                return;

            SpriteRenderer[] renderers = levelRoot.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in renderers)
            {
                Material mat = CreateNeonMaterial(k_Purple, k_WallIntensity);
                sr.material = mat;
            }

            ApplyNeonToCenterLine();
        }

        private void ApplyNeonToCenterLine()
        {
            SpriteRenderer[] allRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            foreach (SpriteRenderer sr in allRenderers)
            {
                if (sr.GetComponent<Ball>() != null)
                    continue;
                if (sr.GetComponent<Paddle>() != null)
                    continue;

                GameObject levelRoot = GameObject.Find(GameSetup.k_RootTransform);
                if (levelRoot != null && sr.transform.IsChildOf(levelRoot.transform))
                    continue;

                Material mat = CreateNeonMaterial(k_Purple, k_WallIntensity * 0.6f);
                sr.material = mat;
            }
        }

        private void SetupBallTrail()
        {
            m_BallTrail = m_Ball.gameObject.AddComponent<TrailRenderer>();
            m_BallTrail.time = k_TrailTime;
            m_BallTrail.startWidth = k_TrailStartWidth;
            m_BallTrail.endWidth = k_TrailEndWidth;
            m_BallTrail.numCornerVertices = 4;
            m_BallTrail.numCapVertices = 4;
            m_BallTrail.material = CreateNeonMaterial(k_Cyan, k_NeonIntensity);
            m_BallTrail.sortingOrder = 1;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(k_Cyan, 0.3f),
                    new GradientColorKey(k_Cyan, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            m_BallTrail.colorGradient = gradient;
            m_BallTrail.enabled = false;
        }

        private void CreateImpactParticles()
        {
            m_ImpactParticles = CreateParticleSystem(
                "NeonImpactParticles",
                k_Cyan,
                k_ImpactParticleCount * 2,
                k_ImpactParticleSpeed,
                k_ImpactParticleLifetime,
                k_ImpactParticleSize,
                k_NeonIntensity
            );
        }

        private void CreateGoalParticles()
        {
            m_GoalParticles = CreateParticleSystem(
                "NeonGoalParticles",
                k_Cyan,
                k_GoalParticleCount * 2,
                k_GoalParticleSpeed,
                k_GoalParticleLifetime,
                k_GoalParticleSize,
                k_NeonIntensity * 1.5f
            );

            var colorOverLifetime = m_GoalParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(k_Cyan, 0.2f),
                    new GradientColorKey(k_Magenta, 0.7f),
                    new GradientColorKey(k_Purple, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.3f),
                    new GradientAlphaKey(0.5f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeOverLifetime = m_GoalParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.3f, 1.5f),
                    new Keyframe(1f, 0f)
                ));
        }

        private ParticleSystem CreateParticleSystem(string name, Color color, int maxParticles,
            float speed, float lifetime, float size, float intensity)
        {
            GameObject go = new GameObject(name);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color * intensity, Color.white * intensity);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.5f, lifetime);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, 1f, 1f, 0f));

            ParticleSystemRenderer psRenderer = go.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = CreateNeonMaterial(color, intensity);
            psRenderer.sortingOrder = 10;

            ps.Stop();
            return ps;
        }

        private void CreateTronGrid()
        {
            m_GridContainer = new GameObject("NeonTronGrid");
            m_GridMaterial = CreateNeonMaterial(k_GridPurple, k_GridIntensity);

            float halfExtent = k_GridLines * k_GridSpacing / 2f;

            for (int i = 0; i <= k_GridLines; i++)
            {
                float pos = -halfExtent + i * k_GridSpacing;
                CreateGridLine(
                    new Vector3(pos, -halfExtent, k_GridZ),
                    new Vector3(pos, halfExtent, k_GridZ),
                    m_GridMaterial
                );
                CreateGridLine(
                    new Vector3(-halfExtent, pos, k_GridZ),
                    new Vector3(halfExtent, pos, k_GridZ),
                    m_GridMaterial
                );
            }
        }

        private void CreateGridLine(Vector3 start, Vector3 end, Material material)
        {
            GameObject lineGO = new GameObject("GridLine");
            lineGO.transform.SetParent(m_GridContainer.transform);

            LineRenderer lr = lineGO.AddComponent<LineRenderer>();
            lr.material = material;
            lr.startWidth = k_GridLineWidth;
            lr.endWidth = k_GridLineWidth;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.useWorldSpace = true;
            lr.sortingOrder = k_GridSortingOrder;
            lr.startColor = Color.white;
            lr.endColor = Color.white;
        }

        // ==================== UPDATE METHODS ====================

        private void UpdateBallSpeedColor()
        {
            if (m_BallMaterial == null || m_BallRigidbody == null || m_BallSpriteRenderer == null)
                return;

            if (!m_BallSpriteRenderer.enabled)
                return;

            float speed = m_BallRigidbody.linearVelocity.magnitude;
            float maxSpeed = m_GameData.MaxSpeed > 0f ? Mathf.Sqrt(m_GameData.MaxSpeed) : 20f;
            float t = Mathf.Clamp01(speed / maxSpeed);

            Color lerpedColor = Color.Lerp(k_Cyan, Color.white, t);
            m_BallMaterial.SetColor("_Color", lerpedColor * k_NeonIntensity);
        }

        private void UpdateBallTrailSync()
        {
            if (m_BallTrail == null || m_BallSpriteRenderer == null)
                return;

            bool ballVisible = m_BallSpriteRenderer.enabled;

            if (!ballVisible && m_BallWasVisible)
            {
                m_BallTrail.Clear();
                m_BallTrail.enabled = false;
            }
            else if (ballVisible && !m_BallWasVisible)
            {
                m_BallTrail.Clear();
                m_BallTrail.enabled = true;
            }

            m_BallWasVisible = ballVisible;
        }

        private void UpdateScreenShake()
        {
            if (m_MainCamera == null)
                return;

            if (m_ShakeTimer > 0f)
            {
                m_ShakeTimer -= Time.deltaTime;
                float progress = Mathf.Clamp01(m_ShakeTimer / m_CurrentShakeDuration);
                float dampedIntensity = m_CurrentShakeIntensity * progress;

                Vector3 shakeOffset = new Vector3(
                    Random.Range(-dampedIntensity, dampedIntensity),
                    Random.Range(-dampedIntensity, dampedIntensity),
                    0f
                );

                m_MainCamera.transform.position = m_CameraOriginalPos + shakeOffset;
            }
            else
            {
                m_MainCamera.transform.position = m_CameraOriginalPos;
            }
        }

        private void UpdateBloomPulse()
        {
            if (m_Bloom == null)
                return;

            if (m_BloomPulseTimer > 0f)
            {
                m_BloomPulseTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(m_BloomPulseTimer / k_BloomPulseDuration);
                float intensity = Mathf.Lerp(k_BloomBaseIntensity, k_BloomPulseIntensity, t);
                m_Bloom.intensity.Override(intensity);
            }
            else
            {
                m_Bloom.intensity.Override(k_BloomBaseIntensity);
            }
        }

        private void UpdateGridPulse()
        {
            if (m_GridMaterial == null)
                return;

            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 0.5f);
            m_GridMaterial.SetColor("_Color", k_GridPurple * k_GridIntensity * pulse);
        }

        // ==================== EVENT HANDLERS ====================

        private void OnBallCollided(Vector2 bounceDirection)
        {
            if (m_Ball == null)
                return;

            Vector3 impactPos = m_Ball.transform.position;

            SpawnImpactParticles(impactPos, bounceDirection);
            TriggerScreenShake(k_ImpactShakeIntensity, k_ImpactShakeDuration);
        }

        private void OnGoalHit(PlayerIDSO playerID)
        {
            if (m_Ball == null)
                return;

            Vector3 explosionPos = m_Ball.transform.position;

            SpawnGoalExplosion(explosionPos);
            TriggerScreenShake(k_GoalShakeIntensity, k_GoalShakeDuration);
        }

        private void OnPointScored(PlayerIDSO playerID)
        {
            m_BloomPulseTimer = k_BloomPulseDuration;
        }

        // ==================== EFFECT TRIGGERS ====================

        private void SpawnImpactParticles(Vector3 position, Vector2 direction)
        {
            if (m_ImpactParticles == null)
                return;

            m_ImpactParticles.transform.position = position;

            if (direction != Vector2.zero)
                m_ImpactParticles.transform.up = direction;

            m_ImpactParticles.Emit(k_ImpactParticleCount);
        }

        private void SpawnGoalExplosion(Vector3 position)
        {
            if (m_GoalParticles == null)
                return;

            m_GoalParticles.transform.position = position;
            m_GoalParticles.Emit(k_GoalParticleCount);
        }

        private void TriggerScreenShake(float intensity, float duration)
        {
            if (intensity > m_CurrentShakeIntensity * (m_ShakeTimer / Mathf.Max(m_CurrentShakeDuration, 0.001f)))
            {
                m_CurrentShakeIntensity = intensity;
                m_CurrentShakeDuration = duration;
                m_ShakeTimer = duration;
            }
        }

        // ==================== HELPERS ====================

        private Material CreateNeonMaterial(Color color, float intensity)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.SetColor("_Color", color * intensity);
            m_CreatedMaterials.Add(mat);
            return mat;
        }
    }
}
