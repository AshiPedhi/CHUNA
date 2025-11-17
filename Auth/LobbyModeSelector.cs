using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CHUNA.Auth
{
    /// <summary>
    /// 로비 모드 선택 관리 클래스 (조 선택 → 사용자 선택)
    ///
    /// [책임]
    /// - 사용자 데이터를 조별로 분류
    /// - 조 선택 패널 UI 관리
    /// - 사용자 선택 패널 UI 관리
    /// - 동적 버튼 생성 및 관리
    ///
    /// [특징]
    /// - 2단계 선택 흐름: 조 → 사용자
    /// - 동적 버튼 생성 및 메모리 관리
    /// - 이벤트 기반 콜백
    /// </summary>
    public class LobbyModeSelector : MonoBehaviour
    {
        #region UI References - Grade Selection
        [Header("=== Grade Selection Panel ===")]
        [SerializeField] private GameObject gradeSelectionPanel;
        [SerializeField] private Image gradeSelectionBackground;
        [SerializeField] private TextMeshProUGUI gradeSelectionTitle;
        [SerializeField] private Button gradeBackButton;
        [SerializeField] private ScrollRect gradeScrollView;
        [SerializeField] private Transform gradeContentContainer;
        [SerializeField] private GameObject gradeButtonPrefab;
        #endregion

        #region UI References - User Selection
        [Header("=== User Selection Panel ===")]
        [SerializeField] private GameObject userSelectionPanel;
        [SerializeField] private Image userSelectionBackground;
        [SerializeField] private TextMeshProUGUI userSelectionTitle;
        [SerializeField] private Button userBackButton;
        [SerializeField] private ScrollRect userScrollView;
        [SerializeField] private Transform userContentContainer;
        [SerializeField] private GameObject userButtonPrefab;
        #endregion

        #region User Data
        private Dictionary<string, List<UserData>> usersByGrade = new Dictionary<string, List<UserData>>();
        private string selectedGrade;
        #endregion

        #region Button Pools
        private List<GameObject> activeGradeButtons = new List<GameObject>();
        private List<GameObject> activeUserButtons = new List<GameObject>();
        #endregion

        #region Events
        /// <summary>
        /// 사용자 선택 시 발생하는 이벤트 (userId, username)
        /// </summary>
        public event Action<int, string> OnUserSelectedEvent;
        #endregion

        #region Public API
        /// <summary>
        /// 초기화 - LobbyAuthUI에서 호출
        /// </summary>
        public void Initialize()
        {
            SetupBackButtons();
            SetupBackgroundDimClicks();
        }

        /// <summary>
        /// 사용자 데이터를 조별로 분류
        /// LobbyAuthUI에서 사용자 목록 로드 후 호출
        /// </summary>
        public void OrganizeUsersByGrade(UserData[] allUsers)
        {
            usersByGrade.Clear();

            if (allUsers == null || allUsers.Length == 0)
            {
                Debug.LogWarning("[ModeSelector] allUsers가 비어있습니다.");
                return;
            }

            foreach (var user in allUsers)
            {
                if (!usersByGrade.ContainsKey(user.grade))
                {
                    usersByGrade[user.grade] = new List<UserData>();
                }
                usersByGrade[user.grade].Add(user);
            }

            Debug.Log($"[ModeSelector] 조별 분류 완료: {usersByGrade.Count}개 조");
        }

        /// <summary>
        /// 조 선택 패널 표시 (진입점)
        /// </summary>
        public void ShowModeSelection()
        {
            ShowGradeSelectionPanel();
        }

        /// <summary>
        /// 모든 패널 숨김
        /// </summary>
        public void HideAllPanels()
        {
            HideGradeSelectionPanel();
            HideUserSelectionPanel();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 뒤로가기 버튼 연결
        /// </summary>
        private void SetupBackButtons()
        {
            if (gradeBackButton != null)
            {
                gradeBackButton.onClick.RemoveAllListeners();
                gradeBackButton.onClick.AddListener(OnGradeBackButtonClicked);
                Debug.Log("[ModeSelector] ✅ 조 뒤로가기 버튼 연결");
            }

            if (userBackButton != null)
            {
                userBackButton.onClick.RemoveAllListeners();
                userBackButton.onClick.AddListener(OnUserBackButtonClicked);
                Debug.Log("[ModeSelector] ✅ 사용자 뒤로가기 버튼 연결");
            }
        }

        /// <summary>
        /// 배경 클릭 시 패널 닫기 설정
        /// </summary>
        private void SetupBackgroundDimClicks()
        {
            // GradeSelectionPanel 배경 클릭
            if (gradeSelectionBackground != null)
            {
                var button = gradeSelectionBackground.GetComponent<Button>();
                if (button == null)
                {
                    button = gradeSelectionBackground.gameObject.AddComponent<Button>();
                }
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => HideGradeSelectionPanel());
                Debug.Log("[ModeSelector] ✅ 조 선택 배경 클릭 연결");
            }

            // UserSelectionPanel 배경 클릭
            if (userSelectionBackground != null)
            {
                var button = userSelectionBackground.GetComponent<Button>();
                if (button == null)
                {
                    button = userSelectionBackground.gameObject.AddComponent<Button>();
                }
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => HideUserSelectionPanel());
                Debug.Log("[ModeSelector] ✅ 사용자 선택 배경 클릭 연결");
            }
        }
        #endregion

        #region Grade Selection Panel
        /// <summary>
        /// 조 선택 패널 표시
        /// </summary>
        private void ShowGradeSelectionPanel()
        {
            if (gradeSelectionPanel == null)
            {
                Debug.LogError("[ModeSelector] gradeSelectionPanel이 null입니다!");
                return;
            }

            gradeSelectionPanel.SetActive(true);
            CreateGradeButtons();
            Debug.Log("[ModeSelector] 조 선택 패널 표시");
        }

        /// <summary>
        /// 조 선택 패널 숨김
        /// </summary>
        private void HideGradeSelectionPanel()
        {
            if (gradeSelectionPanel != null)
            {
                gradeSelectionPanel.SetActive(false);
            }

            ClearGradeButtons();
            Debug.Log("[ModeSelector] 조 선택 패널 숨김");
        }

        /// <summary>
        /// 조 버튼 동적 생성
        /// </summary>
        private void CreateGradeButtons()
        {
            ClearGradeButtons();

            if (gradeButtonPrefab == null || gradeContentContainer == null)
            {
                Debug.LogError("[ModeSelector] gradeButtonPrefab 또는 gradeContentContainer가 null입니다!");
                return;
            }

            foreach (var kvp in usersByGrade)
            {
                string grade = kvp.Key;
                GameObject buttonObj = Instantiate(gradeButtonPrefab, gradeContentContainer);

                // 강제 활성화
                buttonObj.SetActive(true);

                // RectTransform 설정
                var rectTransform = buttonObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(280, 60);
                }

                // LayoutElement 추가/설정
                var layoutElement = buttonObj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = buttonObj.AddComponent<LayoutElement>();
                }
                layoutElement.minHeight = 60;
                layoutElement.preferredHeight = 60;

                var textComponent = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = grade;
                    textComponent.color = Color.white; // 텍스트 색상 명시
                    Debug.Log($"[ModeSelector] 조 버튼 텍스트 설정: {grade}");
                }
                else
                {
                    Debug.LogWarning($"[ModeSelector] 조 버튼에서 TextMeshProUGUI를 찾을 수 없습니다!");
                }

                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnGradeSelected(grade));
                }

                activeGradeButtons.Add(buttonObj);
            }

            Debug.Log($"[ModeSelector] 조 버튼 생성 완료: {activeGradeButtons.Count}개");

            // Content 크기 조정
            AdjustContentSize(gradeContentContainer, activeGradeButtons.Count);
        }

        /// <summary>
        /// 조 버튼 제거
        /// </summary>
        private void ClearGradeButtons()
        {
            foreach (var button in activeGradeButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            activeGradeButtons.Clear();
        }

        /// <summary>
        /// 조 선택 핸들러
        /// </summary>
        private void OnGradeSelected(string grade)
        {
            selectedGrade = grade;
            Debug.Log($"[ModeSelector] 조 선택: {grade}");

            HideGradeSelectionPanel();
            ShowUserSelectionPanel();
        }
        #endregion

        #region User Selection Panel
        /// <summary>
        /// 사용자 선택 패널 표시
        /// </summary>
        private void ShowUserSelectionPanel()
        {
            if (userSelectionPanel == null)
            {
                Debug.LogError("[ModeSelector] userSelectionPanel이 null입니다!");
                return;
            }

            userSelectionPanel.SetActive(true);
            CreateUserButtons();
            Debug.Log("[ModeSelector] 사용자 선택 패널 표시");
        }

        /// <summary>
        /// 사용자 선택 패널 숨김
        /// </summary>
        private void HideUserSelectionPanel()
        {
            if (userSelectionPanel != null)
            {
                userSelectionPanel.SetActive(false);
            }

            ClearUserButtons();
            Debug.Log("[ModeSelector] 사용자 선택 패널 숨김");
        }

        /// <summary>
        /// 사용자 버튼 동적 생성
        /// </summary>
        private void CreateUserButtons()
        {
            ClearUserButtons();

            if (userButtonPrefab == null || userContentContainer == null)
            {
                Debug.LogError("[ModeSelector] userButtonPrefab 또는 userContentContainer가 null입니다!");
                return;
            }

            if (!usersByGrade.ContainsKey(selectedGrade))
            {
                Debug.LogWarning($"[ModeSelector] 선택된 조에 사용자가 없습니다: {selectedGrade}");
                return;
            }

            var users = usersByGrade[selectedGrade];

            foreach (var user in users)
            {
                GameObject buttonObj = Instantiate(userButtonPrefab, userContentContainer);

                // 강제 활성화
                buttonObj.SetActive(true);

                // RectTransform 설정
                var rectTransform = buttonObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(280, 60);
                }

                // LayoutElement 추가/설정
                var layoutElement = buttonObj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = buttonObj.AddComponent<LayoutElement>();
                }
                layoutElement.minHeight = 60;
                layoutElement.preferredHeight = 60;

                var textComponent = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = user.username;
                    textComponent.color = Color.white; // 텍스트 색상 명시
                    Debug.Log($"[ModeSelector] 사용자 버튼 텍스트 설정: {user.username}");
                }
                else
                {
                    Debug.LogWarning($"[ModeSelector] 사용자 버튼에서 TextMeshProUGUI를 찾을 수 없습니다!");
                }

                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    int userId = user.idx;
                    string username = user.username;
                    button.onClick.AddListener(() => OnUserSelected(userId, username));
                }

                activeUserButtons.Add(buttonObj);
            }

            Debug.Log($"[ModeSelector] 사용자 버튼 생성 완료: {activeUserButtons.Count}개");

            // Content 크기 조정
            AdjustContentSize(userContentContainer, activeUserButtons.Count);
        }

        /// <summary>
        /// 사용자 버튼 제거
        /// </summary>
        private void ClearUserButtons()
        {
            foreach (var button in activeUserButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            activeUserButtons.Clear();
        }

        /// <summary>
        /// Content의 크기를 버튼 개수에 맞게 조정
        /// </summary>
        private void AdjustContentSize(Transform content, int buttonCount)
        {
            if (content == null) return;

            var rectTransform = content.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 버튼 높이(60) + 간격(10) * 개수
                float totalHeight = (60 + 10) * buttonCount;
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, totalHeight);

                Debug.Log($"[ModeSelector] Content 크기 조정: {totalHeight}");
            }

            // Vertical Layout Group 추가 (없으면)
            var layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.spacing = 10;
                layout.padding = new RectOffset(10, 10, 10, 10);

                Debug.Log($"[ModeSelector] VerticalLayoutGroup 추가됨");
            }
        }

        /// <summary>
        /// 사용자 선택 핸들러 - 이벤트 발생
        /// </summary>
        private void OnUserSelected(int userId, string username)
        {
            Debug.Log($"[ModeSelector] 사용자 선택: {username} (ID: {userId})");

            // 패널 숨김
            HideUserSelectionPanel();

            // 이벤트 발생 (LobbyAuthUI에서 구독)
            OnUserSelectedEvent?.Invoke(userId, username);
        }
        #endregion

        #region Button Handlers
        /// <summary>
        /// 조 선택 뒤로가기
        /// </summary>
        private void OnGradeBackButtonClicked()
        {
            Debug.Log("[ModeSelector] 조 선택 뒤로가기 클릭");
            HideGradeSelectionPanel();
        }

        /// <summary>
        /// 사용자 선택 뒤로가기
        /// </summary>
        private void OnUserBackButtonClicked()
        {
            Debug.Log("[ModeSelector] 사용자 선택 뒤로가기 클릭");
            HideUserSelectionPanel();
            ShowGradeSelectionPanel();
        }
        #endregion

        #region Debug Helpers
        [ContextMenu("Debug - Show Grade Selection")]
        private void Debug_ShowGradeSelection()
        {
            ShowGradeSelectionPanel();
        }

        [ContextMenu("Debug - Hide All Panels")]
        private void Debug_HideAllPanels()
        {
            HideAllPanels();
        }
        #endregion
    }
}
