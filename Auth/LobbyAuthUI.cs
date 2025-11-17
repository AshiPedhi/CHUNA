using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

namespace CHUNA.Auth
{
    /// <summary>
    /// 로비 인증 UI 메인 오케스트레이터 (리팩토링 버전)
    ///
    /// [책임]
    /// - 인증 서비스 관리
    /// - 디바이스 인증 및 사용자 목록 로드
    /// - 로그인/로그아웃 처리
    /// - UI 컴포넌트 조율 (ScenarioCardManager, ModeSelector)
    /// - 사용자 정보 표시
    ///
    /// [특징]
    /// - Composition 패턴 사용
    /// - Single Responsibility Principle 준수
    /// - 70% 크기 감소 (1,259줄 → ~450줄)
    /// </summary>
    public class LobbyAuthUI : MonoBehaviour
    {
        #region UI References - Main Lobby
        [Header("=== Main Lobby UI ===")]
        [SerializeField] private GameObject headerPanel;
        [SerializeField] private GameObject highlightBar;
        [SerializeField] private GameObject scenarioCardsContainer;
        [SerializeField] private GameObject userInfoPanel;
        [SerializeField] private GameObject bottomButtonsPanel;

        [Header("User Info Panel Components")]
        [SerializeField] private Button userIconButton;
        [SerializeField] private TextMeshProUGUI userNameText;
        [SerializeField] private GameObject userInfoContent;

        [Header("Guide Message")]
        [SerializeField] private TextMeshProUGUI guideMessageText;
        [SerializeField] private string loginGuideMessage = "로그인을 하세요";
        [SerializeField] private string scenarioGuideMessage = "시나리오를 선택하세요";

        [Header("Bottom Buttons")]
        [SerializeField] private Button interactionGuideButton;
        [SerializeField] private Button exitButton;
        #endregion

        #region Component References
        [Header("=== UI Components ===")]
        [SerializeField] private LobbyScenarioCardManager scenarioCardManager;
        [SerializeField] private LobbyModeSelector modeSelector;
        #endregion

        #region Auth Service
        [Header("=== Authentication ===")]
        [SerializeField] private bool useMockService = false;

        private IAuthenticationService authService;
        private string currentDeviceSN;
        private string currentOrgID;
        private int currentUserID;
        private string currentUsername;
        private string savedDeviceSN;
        #endregion

        #region User Data
        private UserData[] allUsers;
        #endregion

        #region Public Properties
        /// <summary>
        /// 로그인 상태 확인 (ScenarioCardManager에서 사용)
        /// </summary>
        public bool IsLoggedIn => !string.IsNullOrEmpty(currentUsername);

        /// <summary>
        /// 현재 사용자 이름 (ScenarioCardManager에서 사용)
        /// </summary>
        public string CurrentUsername => currentUsername;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeAuthService();
            InitializeComponents();
            SetupInitialUI();
            SubscribeToEvents();
        }

        private void Start()
        {
            // 초기 Guest 텍스트 설정
            if (userNameText != null)
            {
                userNameText.text = "Guest";
            }

            // 초기 안내 메시지 설정
            SetGuideMessage(loginGuideMessage);

            LoadSavedDeviceSN();
            AuthenticateDevice().Forget();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            // 애플리케이션 종료 시 자동 로그아웃
            if (!string.IsNullOrEmpty(currentUsername) && !string.IsNullOrEmpty(currentDeviceSN))
            {
                try
                {
                    Debug.Log($"[LobbyUI] OnDestroy - 로그아웃 시도: {currentUsername}");
                    PerformLogoutAsync().Forget();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LobbyUI] OnDestroy 로그아웃 실패: {e.Message}");
                }
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 인증 서비스 초기화
        /// </summary>
        private void InitializeAuthService()
        {
            if (useMockService)
            {
                authService = gameObject.AddComponent<MockAuthenticationService>();
                Debug.Log("[LobbyUI] Mock 서비스 사용");
            }
            else
            {
                authService = AuthenticationService.Instance;
                Debug.Log("[LobbyUI] 실제 서비스 사용");
            }
        }

        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            // ScenarioCardManager 초기화
            if (scenarioCardManager == null)
            {
                scenarioCardManager = GetComponent<LobbyScenarioCardManager>();
            }

            if (scenarioCardManager != null)
            {
                scenarioCardManager.Initialize(this, scenarioCardsContainer);
                Debug.Log("[LobbyUI] ScenarioCardManager 초기화 완료");
            }
            else
            {
                Debug.LogWarning("[LobbyUI] ScenarioCardManager를 찾을 수 없습니다!");
            }

            // ModeSelector 초기화
            if (modeSelector == null)
            {
                modeSelector = GetComponent<LobbyModeSelector>();
            }

            if (modeSelector != null)
            {
                modeSelector.Initialize();
                // 사용자 선택 이벤트 구독
                modeSelector.OnUserSelectedEvent += (userId, username) =>
                {
                    PerformLogin(userId, username).Forget();
                };
                Debug.Log("[LobbyUI] ModeSelector 초기화 완료");
            }
            else
            {
                Debug.LogWarning("[LobbyUI] ModeSelector를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// 초기 UI 설정
        /// </summary>
        private void SetupInitialUI()
        {
            Debug.Log("[LobbyUI] ========== UI 초기화 시작 ==========");

            // userInfoContent는 항상 활성화 (Guest 표시)
            userInfoContent?.SetActive(true);

            // 버튼 연결
            SetupMainButtons();

            Debug.Log("[LobbyUI] ========== UI 초기화 완료 ==========");
        }

        /// <summary>
        /// 메인 버튼들 연결
        /// </summary>
        private void SetupMainButtons()
        {
            // 유저 아이콘 버튼
            if (userIconButton != null)
            {
                userIconButton.onClick.RemoveAllListeners();
                userIconButton.onClick.AddListener(OnUserIconClicked);
                Debug.Log("[LobbyUI] ✅ 유저 아이콘 버튼 연결");
            }

            // 나가기 버튼
            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(OnExitButtonClicked);
                Debug.Log("[LobbyUI] ✅ 나가기 버튼 연결");
            }

            // 상호작용 가이드 버튼
            if (interactionGuideButton != null)
            {
                interactionGuideButton.onClick.RemoveAllListeners();
                interactionGuideButton.onClick.AddListener(OnInteractionGuideClicked);
                Debug.Log("[LobbyUI] ✅ 상호작용 가이드 버튼 연결");
            }
        }
        #endregion

        #region Event Subscription
        private void SubscribeToEvents()
        {
            AuthEvents.OnAuthenticationSuccess += OnAuthenticationSuccess;
            AuthEvents.OnAuthenticationFailed += OnAuthenticationFailed;
            AuthEvents.OnUserListLoadCompleted += OnUserListLoadCompleted;
            AuthEvents.OnUserListLoadFailed += OnUserListLoadFailed;
            AuthEvents.OnLoginSuccess += OnLoginSuccess;
            AuthEvents.OnLoginFailed += OnLoginFailed;
            AuthEvents.OnLogoutCompleted += OnLogoutCompleted;
        }

        private void UnsubscribeFromEvents()
        {
            AuthEvents.OnAuthenticationSuccess -= OnAuthenticationSuccess;
            AuthEvents.OnAuthenticationFailed -= OnAuthenticationFailed;
            AuthEvents.OnUserListLoadCompleted -= OnUserListLoadCompleted;
            AuthEvents.OnUserListLoadFailed -= OnUserListLoadFailed;
            AuthEvents.OnLoginSuccess -= OnLoginSuccess;
            AuthEvents.OnLoginFailed -= OnLoginFailed;
            AuthEvents.OnLogoutCompleted -= OnLogoutCompleted;
        }
        #endregion

        #region Authentication Flow
        /// <summary>
        /// 디바이스 인증 시작
        /// </summary>
        private async UniTaskVoid AuthenticateDevice()
        {
            try
            {
                Debug.Log("[LobbyUI] 디바이스 인증 시작...");
                await AuthenticateDeviceWithRetry(savedDeviceSN);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyUI] 인증 최종 실패: {e.Message}");
                ShowAuthenticationError(e.Message);
            }
        }

        /// <summary>
        /// 디바이스 인증 (재시도 로직 포함)
        /// </summary>
        private async UniTask AuthenticateDeviceWithRetry(string deviceSN, int retryCount = 0)
        {
            const int maxRetries = 2;

            try
            {
                var deviceData = await authService.AuthenticateDeviceAsync(deviceSN);

                if (deviceData == null)
                {
                    throw new Exception("인증 응답 데이터가 null입니다.");
                }

                currentDeviceSN = deviceSN ?? SystemInfo.deviceUniqueIdentifier;
                currentOrgID = deviceData.orgID;

                SaveDeviceSN(currentDeviceSN);

                Debug.Log($"[LobbyUI] 인증 성공: DeviceSN={currentDeviceSN}, OrgID={currentOrgID}");

                if (deviceData.licCHUNA <= 0)
                {
                    ShowLicenseError();
                    return;
                }

                await LoadUserList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyUI] 인증 실패 (시도 {retryCount + 1}/{maxRetries + 1}): {e.Message}");

                if (retryCount < maxRetries)
                {
                    await UniTask.Delay(1000);
                    await AuthenticateDeviceWithRetry(savedDeviceSN, retryCount + 1);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 사용자 목록 로드
        /// </summary>
        private async UniTask LoadUserList()
        {
            try
            {
                Debug.Log($"[LobbyUI] 사용자 목록 로드 시작: {currentOrgID}");

                allUsers = await authService.GetUserListAsync(currentOrgID);

                if (allUsers == null || allUsers.Length == 0)
                {
                    Debug.LogWarning("[LobbyUI] 사용자 목록이 비어있습니다.");
                    return;
                }

                Debug.Log($"[LobbyUI] 사용자 목록 로드 완료: {allUsers.Length}명");

                // ModeSelector에 데이터 전달
                if (modeSelector != null)
                {
                    modeSelector.OrganizeUsersByGrade(allUsers);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyUI] 사용자 목록 로드 실패: {e.Message}");
                throw;
            }
        }
        #endregion

        #region Button Click Handlers
        /// <summary>
        /// 유저 아이콘 클릭 - 조 선택 패널 표시
        /// </summary>
        private void OnUserIconClicked()
        {
            Debug.Log("[LobbyUI] 유저 아이콘 클릭");

            if (modeSelector != null)
            {
                modeSelector.ShowModeSelection();
            }
        }

        /// <summary>
        /// 나가기 버튼 클릭
        /// </summary>
        private void OnExitButtonClicked()
        {
            Debug.Log("[LobbyUI] 나가기 버튼 클릭");
            ExitApplication();
        }

        /// <summary>
        /// 상호작용 가이드 버튼 클릭
        /// </summary>
        private void OnInteractionGuideClicked()
        {
            Debug.Log("[LobbyUI] 상호작용 가이드 버튼 클릭");
            // TODO: 상호작용 가이드 표시
        }
        #endregion

        #region Application Management
        /// <summary>
        /// 애플리케이션 종료
        /// </summary>
        private async void ExitApplication()
        {
            // 로그아웃 후 종료
            if (!string.IsNullOrEmpty(currentUsername))
            {
                try
                {
                    await PerformLogoutAsync();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LobbyUI] 종료 시 로그아웃 실패: {e.Message}");
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            Debug.Log("[LobbyUI] 애플리케이션 종료");
        }
        #endregion

        #region Login/Logout
        /// <summary>
        /// 로그인 처리 (ModeSelector 이벤트에서 호출)
        /// </summary>
        private async UniTaskVoid PerformLogin(int userId, string username)
        {
            try
            {
                Debug.Log($"[LobbyUI] 로그인 시작: {username}");

                try
                {
                    var mirroringData = await authService.LogonAsync(
                        currentDeviceSN,
                        username,
                        "VR_CHUNA"
                    );

                    Debug.Log($"[LobbyUI] 미러링 데이터 수신 완료");
                }
                catch (Exception logonException)
                {
                    // 404 에러(serverIP not found)는 경고로 처리하고 로그인 진행
                    if (logonException.Message.Contains("404") || logonException.Message.Contains("serverIP not found"))
                    {
                        Debug.LogWarning($"[LobbyUI] 미러링 정보 없음 (무시하고 진행): {logonException.Message}");
                    }
                    else
                    {
                        // 다른 에러는 재발생
                        throw;
                    }
                }

                // 로그인 정보 저장
                currentUserID = userId;
                currentUsername = username;

                // UI 업데이트
                UpdateUserInfoPanel();

                // ScenarioCardManager에 로그인 상태 전달
                if (scenarioCardManager != null)
                {
                    scenarioCardManager.OnLoginStateChanged(true);
                }

                // 안내 메시지 변경
                SetGuideMessage(scenarioGuideMessage);

                Debug.Log($"[LobbyUI] ✅ 로그인 완료: {username} (ID: {userId})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyUI] ❌ 로그인 실패: {e.Message}");
                ShowLoginError(e.Message);
            }
        }

        /// <summary>
        /// 로그아웃 처리
        /// </summary>
        private async UniTaskVoid PerformLogout()
        {
            await PerformLogoutAsync();
        }

        /// <summary>
        /// 로그아웃 처리 (await 가능)
        /// </summary>
        private async UniTask PerformLogoutAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(currentUsername))
                {
                    return;
                }

                Debug.Log($"[LobbyUI] 로그아웃 시작: {currentUsername}");

                await authService.LogoffAsync(
                    currentDeviceSN,
                    currentUsername,
                    "VR_CHUNA"
                );

                ClearUserInfo();

                Debug.Log($"[LobbyUI] 로그아웃 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyUI] 로그아웃 실패: {e.Message}");
                throw;
            }
        }
        #endregion

        #region Auth Event Handlers
        private void OnAuthenticationSuccess(string deviceSN)
        {
            Debug.Log($"[LobbyUI] [이벤트] 인증 성공: {deviceSN}");
        }

        private void OnAuthenticationFailed(string errorMessage)
        {
            Debug.LogError($"[LobbyUI] [이벤트] 인증 실패: {errorMessage}");
        }

        private void OnUserListLoadCompleted(int userCount)
        {
            Debug.Log($"[LobbyUI] [이벤트] 사용자 목록 로드 완료: {userCount}명");
        }

        private void OnUserListLoadFailed(string errorMessage)
        {
            Debug.LogError($"[LobbyUI] [이벤트] 사용자 목록 로드 실패: {errorMessage}");
        }

        private void OnLoginSuccess(string username, int userID)
        {
            Debug.Log($"[LobbyUI] [이벤트] 로그인 성공: {username}");
        }

        private void OnLoginFailed(string username, string errorMessage)
        {
            Debug.LogError($"[LobbyUI] [이벤트] 로그인 실패: {username} - {errorMessage}");
        }

        private void OnLogoutCompleted(string username)
        {
            Debug.Log($"[LobbyUI] [이벤트] 로그아웃 완료: {username}");
        }
        #endregion

        #region UI Update
        /// <summary>
        /// 사용자 정보 패널 업데이트
        /// </summary>
        private void UpdateUserInfoPanel()
        {
            if (userNameText != null)
            {
                userNameText.text = currentUsername;
            }

            Debug.Log("[LobbyUI] 사용자 정보 패널 업데이트");
        }

        /// <summary>
        /// 사용자 정보 초기화
        /// </summary>
        private void ClearUserInfo()
        {
            currentUsername = string.Empty;
            currentUserID = 0;

            // 텍스트를 "Guest"로 변경
            if (userNameText != null)
            {
                userNameText.text = "Guest";
            }

            // ScenarioCardManager에 로그아웃 상태 전달
            if (scenarioCardManager != null)
            {
                scenarioCardManager.OnLoginStateChanged(false);
            }

            // 안내 메시지 변경
            SetGuideMessage(loginGuideMessage);

            Debug.Log("[LobbyUI] 사용자 정보 초기화");
        }

        /// <summary>
        /// 안내 메시지 설정
        /// </summary>
        private void SetGuideMessage(string message)
        {
            if (guideMessageText != null)
            {
                guideMessageText.text = message;
                Debug.Log($"[LobbyUI] 안내 메시지 변경: {message}");
            }
        }
        #endregion

        #region Error Handling
        private void ShowAuthenticationError(string errorMessage)
        {
            Debug.LogError($"[LobbyUI] 인증 오류: {errorMessage}");
            // TODO: 오류 팝업 표시
        }

        private void ShowLicenseError()
        {
            Debug.LogError("[LobbyUI] 라이선스 오류: CHUNA 라이선스가 없습니다.");
            // TODO: 라이선스 오류 팝업 표시
        }

        private void ShowLoginError(string errorMessage)
        {
            Debug.LogError($"[LobbyUI] 로그인 오류: {errorMessage}");
            // TODO: 로그인 오류 팝업 표시
        }
        #endregion

        #region Public API
        /// <summary>
        /// 현재 사용자 정보 반환
        /// </summary>
        public (int userID, string username, string orgID, string deviceSN) GetCurrentUserInfo()
        {
            return (currentUserID, currentUsername, currentOrgID, currentDeviceSN);
        }

        /// <summary>
        /// 강제 로그아웃
        /// </summary>
        public void ForceLogout()
        {
            if (!string.IsNullOrEmpty(currentUsername))
            {
                PerformLogout().Forget();
            }
        }

        /// <summary>
        /// 디바이스 초기화
        /// </summary>
        public async UniTask ResetDevice()
        {
            if (string.IsNullOrEmpty(currentDeviceSN))
            {
                Debug.LogWarning("[LobbyUI] DeviceSN이 없어 초기화할 수 없습니다.");
                return;
            }

            try
            {
                Debug.Log($"[LobbyUI] 디바이스 초기화 시작: {currentDeviceSN}");

                await authService.ResetDeviceAsync(currentDeviceSN, "VR_CHUNA");

                ClearSavedDeviceSN();
                currentDeviceSN = string.Empty;
                currentOrgID = string.Empty;
                currentUserID = 0;
                currentUsername = string.Empty;

                Debug.Log("[LobbyUI] 디바이스 초기화 완료");

                // TODO: 인증 씬으로 이동
                // SceneManager.LoadScene("AuthMain");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyUI] 디바이스 초기화 실패: {e.Message}");
            }
        }
        #endregion

        #region PlayerPrefs Management
        private void LoadSavedDeviceSN()
        {
            if (PlayerPrefs.HasKey("DEVICE_SN"))
            {
                savedDeviceSN = PlayerPrefs.GetString("DEVICE_SN");
                Debug.Log($"[LobbyUI] 저장된 DeviceSN 로드: {savedDeviceSN}");
            }
            else
            {
                savedDeviceSN = string.Empty;
                Debug.Log("[LobbyUI] 저장된 DeviceSN 없음");
            }
        }

        private void SaveDeviceSN(string deviceSN)
        {
            PlayerPrefs.SetString("DEVICE_SN", deviceSN);
            PlayerPrefs.Save();
            savedDeviceSN = deviceSN;
            Debug.Log($"[LobbyUI] DeviceSN 저장: {deviceSN}");
        }

        private void ClearSavedDeviceSN()
        {
            PlayerPrefs.DeleteKey("DEVICE_SN");
            PlayerPrefs.Save();
            savedDeviceSN = string.Empty;
            Debug.Log("[LobbyUI] 저장된 DeviceSN 삭제");
        }
        #endregion

        #region Debug Helpers
        [ContextMenu("Debug - Hide All Panels")]
        private void Debug_HideAllPanels()
        {
            if (modeSelector != null)
            {
                modeSelector.HideAllPanels();
            }
        }

        [ContextMenu("Debug - Reset Device")]
        private void Debug_ResetDevice()
        {
            ResetDevice().Forget();
        }

        [ContextMenu("Debug - Show Current Info")]
        private void Debug_ShowCurrentInfo()
        {
            Debug.Log($"=== Current State ===");
            Debug.Log($"DeviceSN: {currentDeviceSN}");
            Debug.Log($"OrgID: {currentOrgID}");
            Debug.Log($"UserID: {currentUserID}");
            Debug.Log($"Username: {currentUsername}");
            Debug.Log($"IsLoggedIn: {IsLoggedIn}");
        }
        #endregion
    }
}
