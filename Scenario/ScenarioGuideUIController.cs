using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 시나리오 가이드 UI 전용 컨트롤러
/// - stepName 표시
/// - Phase 이미지 (중부/전부/후부) 진행 상태 표시
/// - 시작 토글 제어 (가이드 스텝에서만 표시)
/// </summary>
public class ScenarioGuideUIController : MonoBehaviour
{
    [Header("=== Step Name 표시 ===")]
    [SerializeField] private TextMeshProUGUI stepNameText;

    [Header("=== Phase 이미지 (진행 상태 표시) ===")]
    [SerializeField] private Image middlePhaseImage;  // 중부
    [SerializeField] private Image frontPhaseImage;   // 전부
    [SerializeField] private Image backPhaseImage;    // 후부

    [Header("=== Phase 이미지 색상 ===")]
    [SerializeField] private Color activePhaseColor = new Color(0.3f, 0.6f, 1f, 1f);     // 활성화된 Phase (파란색)
    [SerializeField] private Color inactivePhaseColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 비활성 Phase (회색)

    [Header("=== 시작 토글 ===")]
    [SerializeField] private GameObject startToggleObject;
    [SerializeField] private Toggle startToggle;
    [SerializeField] private TextMeshProUGUI startToggleText;

    [Header("=== 설명 텍스트 ===")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    private ScenarioEventSystem eventSystem;
    private ScenarioManager scenarioManager;
    private string currentPhaseName = "";

    void Awake()
    {
        eventSystem = ScenarioEventSystem.Instance;
        scenarioManager = FindObjectOfType<ScenarioManager>();

        // 시작 토글 이벤트 연결
        if (startToggle != null)
        {
            startToggle.onValueChanged.AddListener(OnStartToggleChanged);
        }
    }

    void OnEnable()
    {
        // 이벤트 구독
        eventSystem.OnPhaseChanged += OnPhaseChanged;
        eventSystem.OnStepChanged += OnStepChanged;
        eventSystem.OnSubStepStarted += OnSubStepStarted;
    }

    void OnDisable()
    {
        // 이벤트 구독 해제
        eventSystem.OnPhaseChanged -= OnPhaseChanged;
        eventSystem.OnStepChanged -= OnStepChanged;
        eventSystem.OnSubStepStarted -= OnSubStepStarted;
    }

    /// <summary>
    /// Phase 변경 시 호출
    /// </summary>
    private void OnPhaseChanged(PhaseData phase)
    {
        currentPhaseName = phase.phaseName;
        UpdatePhaseImages();

        Debug.Log($"[GuideUI] Phase 변경: {currentPhaseName}");
    }

    /// <summary>
    /// Step 변경 시 호출
    /// </summary>
    private void OnStepChanged(StepData step)
    {
        UpdateStepName(step.stepName);
        UpdateStartToggleVisibility(step);

        Debug.Log($"[GuideUI] Step 변경: {step.stepName}");
    }

    /// <summary>
    /// SubStep 시작 시 호출
    /// </summary>
    private void OnSubStepStarted(SubStepData subStep)
    {
        UpdateDescription(subStep.voiceInstruction);
    }

    /// <summary>
    /// Step 이름 업데이트
    /// </summary>
    private void UpdateStepName(string stepName)
    {
        if (stepNameText != null)
        {
            stepNameText.text = stepName;
        }
    }

    /// <summary>
    /// 설명 텍스트 업데이트
    /// </summary>
    private void UpdateDescription(string description)
    {
        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
    }

    /// <summary>
    /// Phase 이미지 색상 업데이트
    /// </summary>
    private void UpdatePhaseImages()
    {
        // 중부
        if (middlePhaseImage != null)
        {
            UpdateImageColor(middlePhaseImage, currentPhaseName == "중부");
        }

        // 전부
        if (frontPhaseImage != null)
        {
            UpdateImageColor(frontPhaseImage, currentPhaseName == "전부");
        }

        // 후부
        if (backPhaseImage != null)
        {
            UpdateImageColor(backPhaseImage, currentPhaseName == "후부");
        }
    }

    /// <summary>
    /// 이미지 색상 업데이트
    /// </summary>
    private void UpdateImageColor(Image image, bool isActive)
    {
        if (image == null) return;

        image.color = isActive ? activePhaseColor : inactivePhaseColor;
    }

    /// <summary>
    /// 시작 토글 표시 여부 업데이트
    /// </summary>
    private void UpdateStartToggleVisibility(StepData step)
    {
        if (startToggleObject == null) return;

        // 가이드 스텝(stepNo == 0)에서만 시작 토글 표시
        bool shouldShow = step.IsGuideStep();
        startToggleObject.SetActive(shouldShow);

        // 토글 텍스트 업데이트
        if (shouldShow && startToggleText != null)
        {
            // 첫 번째 가이드인지 확인
            bool isFirstPhase = scenarioManager.CurrentPhase == scenarioManager.CurrentScenario.phases[0];
            startToggleText.text = isFirstPhase ? "시작하기" : "다음 단계 시작";
        }

        // 토글 상태 초기화 (꺼진 상태로)
        if (shouldShow && startToggle != null)
        {
            startToggle.isOn = false;
        }
    }

    /// <summary>
    /// 시작 토글 변경 시
    /// </summary>
    private void OnStartToggleChanged(bool isOn)
    {
        // 토글이 켜졌을 때만 다음 단계로 진행
        if (isOn && scenarioManager != null)
        {
            Debug.Log("[GuideUI] 시작 토글 클릭 - 다음 SubStep으로 진행");
            scenarioManager.NextSubStep();
        }
    }

    /// <summary>
    /// 수동으로 Step 이름 설정
    /// </summary>
    public void SetStepName(string stepName)
    {
        UpdateStepName(stepName);
    }

    /// <summary>
    /// 수동으로 Phase 설정
    /// </summary>
    public void SetCurrentPhase(string phaseName)
    {
        currentPhaseName = phaseName;
        UpdatePhaseImages();
    }

    /// <summary>
    /// Phase 이미지 색상 설정
    /// </summary>
    public void SetPhaseColors(Color activeColor, Color inactiveColor)
    {
        activePhaseColor = activeColor;
        inactivePhaseColor = inactiveColor;
        UpdatePhaseImages();
    }
}