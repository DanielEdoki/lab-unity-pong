using UnityEngine;
using UnityEngine.UIElements;

namespace GameSystemsCookbook.Demos.PaddleBall
{
    /// <summary>
    /// Manages the AI difficulty toggle buttons on the Play Select screen.
    /// Sets AIPaddleSettingsSO.ActiveDifficulty based on user selection.
    /// </summary>
    public class DifficultySelector : MonoBehaviour
    {
        private const string k_SelectedClass = "difficulty-button--selected";

        [Tooltip("The UI Toolkit document containing the difficulty buttons")]
        [SerializeField] private UIDocument m_Document;

        private Button m_BadButton;
        private Button m_CasualButton;
        private Button m_ProButton;
        private Button m_CurrentSelection;
        private bool m_Initialized;

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            // Re-initialize when re-enabled (e.g. after scene transitions)
            if (m_Initialized)
                Initialize();
        }

        private void Initialize()
        {
            if (m_Document == null)
                m_Document = GetComponent<UIDocument>();

            if (m_Document == null)
            {
                Debug.LogWarning("[DifficultySelector] No UIDocument found");
                return;
            }

            var root = m_Document.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("[DifficultySelector] rootVisualElement is null");
                return;
            }

            m_BadButton = root.Q<Button>("difficulty__button-bad");
            m_CasualButton = root.Q<Button>("difficulty__button-casual");
            m_ProButton = root.Q<Button>("difficulty__button-pro");

            if (m_BadButton == null || m_CasualButton == null || m_ProButton == null)
            {
                Debug.LogWarning("[DifficultySelector] Could not find difficulty buttons in UXML");
                return;
            }

            // Unregister first to avoid duplicate handlers on re-init
            m_BadButton.clicked -= OnBadClicked;
            m_CasualButton.clicked -= OnCasualClicked;
            m_ProButton.clicked -= OnProClicked;

            m_BadButton.clicked += OnBadClicked;
            m_CasualButton.clicked += OnCasualClicked;
            m_ProButton.clicked += OnProClicked;

            m_Initialized = true;

            // Restore visual state
            RestoreSelection();

            Debug.Log($"[DifficultySelector] Initialized. Active difficulty: {AIPaddleSettingsSO.ActiveDifficulty}");
        }

        private void OnDisable()
        {
            if (m_BadButton != null) m_BadButton.clicked -= OnBadClicked;
            if (m_CasualButton != null) m_CasualButton.clicked -= OnCasualClicked;
            if (m_ProButton != null) m_ProButton.clicked -= OnProClicked;
        }

        private void OnBadClicked() => SelectDifficulty(AIDifficulty.Bad, m_BadButton);
        private void OnCasualClicked() => SelectDifficulty(AIDifficulty.Casual, m_CasualButton);
        private void OnProClicked() => SelectDifficulty(AIDifficulty.Pro, m_ProButton);

        private void SelectDifficulty(AIDifficulty difficulty, Button button)
        {
            // Toggle: clicking the same button deselects it (returns to Custom)
            if (m_CurrentSelection == button)
            {
                button.RemoveFromClassList(k_SelectedClass);
                m_CurrentSelection = null;
                AIPaddleSettingsSO.ActiveDifficulty = AIDifficulty.Custom;
                Debug.Log("[DifficultySelector] Difficulty set to: Custom");
                return;
            }

            // Deselect previous
            if (m_CurrentSelection != null)
                m_CurrentSelection.RemoveFromClassList(k_SelectedClass);

            // Select new
            button.AddToClassList(k_SelectedClass);
            m_CurrentSelection = button;
            AIPaddleSettingsSO.ActiveDifficulty = difficulty;
            Debug.Log($"[DifficultySelector] Difficulty set to: {difficulty}");
        }

        private void RestoreSelection()
        {
            // Clear any existing selection visuals
            m_BadButton.RemoveFromClassList(k_SelectedClass);
            m_CasualButton.RemoveFromClassList(k_SelectedClass);
            m_ProButton.RemoveFromClassList(k_SelectedClass);
            m_CurrentSelection = null;

            switch (AIPaddleSettingsSO.ActiveDifficulty)
            {
                case AIDifficulty.Bad:
                    SelectDifficulty(AIDifficulty.Bad, m_BadButton);
                    break;
                case AIDifficulty.Casual:
                    SelectDifficulty(AIDifficulty.Casual, m_CasualButton);
                    break;
                case AIDifficulty.Pro:
                    SelectDifficulty(AIDifficulty.Pro, m_ProButton);
                    break;
            }
        }
    }
}
