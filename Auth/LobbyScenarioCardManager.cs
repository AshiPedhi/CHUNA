using UnityEngine;
using UnityEngine.UI;

namespace CHUNA.Auth
{
    /// <summary>
    /// 시나리오 카드 UI 관리 클래스
    ///
    /// [책임]
    /// - 시나리오 카드 버튼 자동 검색 및 연결
    /// - 시나리오 카드 클릭 처리 및 로그인 체크
    /// - 시나리오 카드 시각적 상태 관리
    /// - 로그인 필요 팝업 표시
    ///
    /// [특징]
    /// - 시나리오 카드는 항상 클릭 가능 (로그인 안 되면 팝업)
    /// - 로그인 상태에 따라 시각적 피드백 제공
    /// </summary>
    public class LobbyScenarioCardManager : MonoBehaviour
    {
        #region UI References
        [Header("=== Scenario Cards ===")]
        [SerializeField] private Button[] scenarioCardButtons = new Button[5];
        [SerializeField] private CanvasGroup[] scenarioCardCanvasGroups = new CanvasGroup[5];
        [SerializeField] private GameObject scenarioCardsContainer;

        [Header("Settings")]
        [Tooltip("시나리오 카드를 자동으로 찾아서 연결할지 여부")]
        [SerializeField] private bool autoFindScenarioCards = true;

        [Header("Popups")]
        [SerializeField] private GameObject loginRequiredPopup;
        [SerializeField] private Button loginRequiredCloseButton;
        #endregion

        #region Dependencies
        private LobbyAuthUI mainUI;
        #endregion

        #region Public API
        /// <summary>
        /// 초기화 - LobbyAuthUI에서 호출
        /// </summary>
        public void Initialize(LobbyAuthUI main, GameObject container)
        {
            mainUI = main;
            scenarioCardsContainer = container;

            // 자동 검색이 활성화되어 있으면 카드 검색
            if (autoFindScenarioCards)
            {
                AutoFindScenarioCards();
            }

            // 시나리오 카드 버튼 연결
            SetupScenarioCards();

            // 로그인 팝업 닫기 버튼 연결
            if (loginRequiredCloseButton != null)
            {
                loginRequiredCloseButton.onClick.AddListener(OnLoginRequiredPopupClose);
            }
        }

        /// <summary>
        /// 로그인 상태 변경 시 호출 (LobbyAuthUI에서 호출)
        /// </summary>
        public void OnLoginStateChanged(bool isLoggedIn)
        {
            SetScenarioCardsVisualState(isLoggedIn);
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 시나리오 카드 자동 검색
        /// </summary>
        private void AutoFindScenarioCards()
        {
            if (scenarioCardsContainer == null)
            {
                Debug.LogWarning("[ScenarioCardManager] scenarioCardsContainer가 null입니다. 자동 검색을 건너뜁니다.");
                return;
            }

            Debug.Log("[ScenarioCardManager] 시나리오 카드 자동 검색 시작...");

            // Card_01 ~ Card_05 찾기
            for (int i = 0; i < 5; i++)
            {
                string cardName = $"Card_{(i + 1):00}";
                Transform cardTransform = scenarioCardsContainer.transform.Find(cardName);

                if (cardTransform != null)
                {
                    // Button 컴포넌트 찾기
                    Button cardButton = cardTransform.GetComponent<Button>();
                    if (cardButton == null)
                    {
                        cardButton = cardTransform.GetComponentInChildren<Button>();
                    }

                    if (cardButton != null)
                    {
                        scenarioCardButtons[i] = cardButton;
                        Debug.Log($"[ScenarioCardManager] ✅ {cardName} 버튼 자동 연결 성공");
                    }
                    else
                    {
                        Debug.LogWarning($"[ScenarioCardManager] ⚠️ {cardName}에서 Button 컴포넌트를 찾을 수 없습니다.");
                    }

                    // CanvasGroup 찾기
                    CanvasGroup canvasGroup = cardTransform.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        scenarioCardCanvasGroups[i] = canvasGroup;
                        Debug.Log($"[ScenarioCardManager] ✅ {cardName} CanvasGroup 자동 연결 성공");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ScenarioCardManager] ⚠️ {cardName}을(를) 찾을 수 없습니다.");
                }
            }
        }

        /// <summary>
        /// 시나리오 카드 버튼 연결 (항상 클릭 가능)
        /// </summary>
        private void SetupScenarioCards()
        {
            Debug.Log("[ScenarioCardManager] --- 시나리오 카드 버튼 연결 시작 ---");

            int connectedCount = 0;

            for (int i = 0; i < scenarioCardButtons.Length; i++)
            {
                if (scenarioCardButtons[i] != null)
                {
                    int index = i; // 클로저를 위한 로컬 복사

                    // 기존 리스너 제거
                    scenarioCardButtons[i].onClick.RemoveAllListeners();

                    // 새 리스너 추가
                    scenarioCardButtons[i].onClick.AddListener(() => OnScenarioCardClicked(index));

                    // 버튼은 항상 활성화
                    scenarioCardButtons[i].interactable = true;

                    Debug.Log($"[ScenarioCardManager] ✅ 시나리오 카드 {index + 1} 버튼 연결 완료");
                    connectedCount++;
                }
                else
                {
                    Debug.LogError($"[ScenarioCardManager] ❌ 시나리오 카드 {i + 1} 버튼이 NULL입니다!");
                }
            }

            // CanvasGroup 설정 (시각적으로만 비활성화, 클릭은 가능)
            for (int i = 0; i < scenarioCardCanvasGroups.Length; i++)
            {
                if (scenarioCardCanvasGroups[i] != null)
                {
                    scenarioCardCanvasGroups[i].alpha = 0.5f; // 시각적으로 어둡게
                    scenarioCardCanvasGroups[i].interactable = true; // 클릭 가능
                    scenarioCardCanvasGroups[i].blocksRaycasts = true; // 레이캐스트 차단 안 함
                }
            }

            Debug.Log($"[ScenarioCardManager] 시나리오 카드 버튼 연결 완료: {connectedCount}/5");
        }
        #endregion

        #region Button Click Handlers
        /// <summary>
        /// 시나리오 카드 클릭 핸들러
        /// </summary>
        public void OnScenarioCardClicked(int scenarioIndex)
        {
            Debug.Log($"[ScenarioCardManager] ========== 시나리오 카드 {scenarioIndex + 1} 클릭 ==========");

            // 로그인 체크 - mainUI를 통해 확인
            if (mainUI == null || !mainUI.IsLoggedIn)
            {
                Debug.LogWarning("[ScenarioCardManager] ⚠️ 로그인이 필요합니다!");
                ShowLoginRequiredPopup();
                return;
            }

            Debug.Log($"[ScenarioCardManager] ✅ 시나리오 {scenarioIndex + 1} 시작: 사용자={mainUI.CurrentUsername}");

            // TODO: 시나리오 씬 로드
            // SceneManager.LoadScene($"Scenario_{scenarioIndex + 1}");
        }
        #endregion

        #region UI Update
        /// <summary>
        /// 시나리오 카드 시각 상태 설정
        /// </summary>
        private void SetScenarioCardsVisualState(bool active)
        {
            float targetAlpha = active ? 1f : 0.5f;

            // CanvasGroup으로 알파 조절 (클릭은 항상 가능)
            foreach (var canvasGroup in scenarioCardCanvasGroups)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = targetAlpha;
                    // interactable과 blocksRaycasts는 항상 true (항상 클릭 가능)
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }

            Debug.Log($"[ScenarioCardManager] 시나리오 카드 시각 상태: {(active ? "활성" : "비활성")} (Alpha: {targetAlpha})");
        }

        /// <summary>
        /// 로그인 필요 팝업 표시
        /// </summary>
        private void ShowLoginRequiredPopup()
        {
            if (loginRequiredPopup != null)
            {
                loginRequiredPopup.SetActive(true);
                Debug.Log("[ScenarioCardManager] 로그인 필요 팝업 표시");
            }
            else
            {
                Debug.LogWarning("[ScenarioCardManager] loginRequiredPopup이 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// 로그인 필요 팝업 닫기
        /// </summary>
        private void OnLoginRequiredPopupClose()
        {
            if (loginRequiredPopup != null)
            {
                loginRequiredPopup.SetActive(false);
                Debug.Log("[ScenarioCardManager] 로그인 필요 팝업 닫기");
            }
        }
        #endregion

        #region Debug Helpers
        [ContextMenu("Debug - Show Login Popup")]
        private void Debug_ShowLoginPopup()
        {
            ShowLoginRequiredPopup();
        }

        [ContextMenu("Debug - Test Scenario Click (Not Logged In)")]
        private void Debug_TestScenarioClickNotLoggedIn()
        {
            // 로그아웃 상태로 강제 테스트
            OnScenarioCardClicked(0);
        }

        [ContextMenu("Debug - Test Scenario Click (Logged In)")]
        private void Debug_TestScenarioClickLoggedIn()
        {
            // mainUI가 로그인된 상태여야 테스트 가능
            if (mainUI != null && mainUI.IsLoggedIn)
            {
                OnScenarioCardClicked(0);
            }
            else
            {
                Debug.LogWarning("[ScenarioCardManager] 테스트 실패: mainUI가 로그인되지 않았습니다.");
            }
        }
        #endregion
    }
}
