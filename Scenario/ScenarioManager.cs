using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Inspector에서 직접 편집 가능한 시나리오 매니저
/// 모드 선택 정보 저장 기능 추가
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    [Header("=== 프로토타입 시나리오 데이터 ===")]
    [Tooltip("프로토타입용 시나리오 (Inspector에서 직접 편집)")]
    [SerializeField] private ScenarioData prototypeScenario;

    [Header("=== CSV 로드 설정 ===")]
    [Tooltip("CSV 파일을 사용할지 여부")]
    [SerializeField] private bool useCSVData = false;

    [Tooltip("CSV 파일 이름 (Resources/Scenarios/ 폴더)")]
    [SerializeField] private string csvFileName = "ScenarioData";

    [Header("=== 디버그 ===")]
    [SerializeField] private bool showDebugLog = true;

    // 현재 진행 상태
    private ScenarioData currentScenario;
    private PhaseData currentPhase;
    private StepData currentStep;
    private SubStepData currentSubStep;

    // 인덱스
    private int currentPhaseIndex = 0;
    private int currentStepIndex = 0;
    private int currentSubStepIndex = 0;

    // 이벤트 시스템
    private ScenarioEventSystem eventSystem;

    // 선택된 모드 정보
    private string selectedMode = "";
    private string selectedDifficulty = "";

    // 프로퍼티
    public ScenarioData CurrentScenario => currentScenario;
    public PhaseData CurrentPhase => currentPhase;
    public StepData CurrentStep => currentStep;
    public SubStepData CurrentSubStep => currentSubStep;
    public bool IsLastSubStep => currentSubStepIndex >= currentStep.subSteps.Count - 1;
    public bool IsLastStep => currentStepIndex >= currentPhase.steps.Count - 1;
    public bool IsLastPhase => currentPhaseIndex >= currentScenario.phases.Count - 1;

    // 모드 정보 프로퍼티
    public string SelectedMode => selectedMode;
    public string SelectedDifficulty => selectedDifficulty;

    private void Awake()
    {
        eventSystem = ScenarioEventSystem.Instance;
    }

    /// <summary>
    /// 모드와 난이도 정보 설정
    /// </summary>
    public void SetModeInfo(string mode, string difficulty)
    {
        selectedMode = mode;
        selectedDifficulty = difficulty;

        Debug.Log($"[ScenarioManager] 모드 설정: {mode}, 난이도: {difficulty}");
    }

    /// <summary>
    /// 시나리오 시작 (프로토타입 또는 CSV)
    /// </summary>
    public void StartScenario()
    {
        if (useCSVData)
        {
            LoadFromCSV();
        }
        else
        {
            if (prototypeScenario == null)
            {
                LogError("프로토타입 시나리오가 설정되지 않았습니다!");
                return;
            }

            StartScenario(prototypeScenario);
        }
    }

    /// <summary>
    /// 특정 시나리오 시작
    /// </summary>
    public void StartScenario(ScenarioData scenario)
    {
        if (scenario == null || scenario.phases.Count == 0)
        {
            LogError("유효하지 않은 시나리오 데이터입니다!");
            return;
        }

        currentScenario = scenario;
        currentPhaseIndex = 0;
        currentStepIndex = 0;
        currentSubStepIndex = 0;

        currentPhase = currentScenario.phases[0];
        currentStep = currentPhase.steps[0];
        currentSubStep = currentStep.subSteps[0];

        // 이벤트 발생
        eventSystem.ScenarioStarted(currentScenario);
        eventSystem.PhaseChanged(currentPhase);
        eventSystem.StepChanged(currentStep);
        eventSystem.SubStepStarted(currentSubStep);

        UpdateUI();
        UpdateProgress();

        Log($"시나리오 시작: {currentScenario.scenarioName} (모드: {selectedMode}, 난이도: {selectedDifficulty})");
    }

    /// <summary>
    /// CSV에서 로드
    /// </summary>
    private void LoadFromCSV()
    {
        ScenarioCSVLoader loader = GetComponent<ScenarioCSVLoader>();
        if (loader == null)
        {
            loader = gameObject.AddComponent<ScenarioCSVLoader>();
        }

        ScenarioCollection collection = loader.LoadScenarios(csvFileName);

        if (collection == null || collection.scenarios.Count == 0)
        {
            LogError("CSV 로드 실패!");
            return;
        }

        StartScenario(collection.scenarios[0]);
    }

    /// <summary>
    /// 다음 SubStep으로 진행
    /// </summary>
    public void NextSubStep()
    {
        if (currentSubStep != null)
        {
            eventSystem.SubStepCompleted(currentSubStep);
        }

        // 다음 SubStep이 있으면 진행
        if (currentSubStepIndex < currentStep.subSteps.Count - 1)
        {
            currentSubStepIndex++;
            currentSubStep = currentStep.subSteps[currentSubStepIndex];

            eventSystem.SubStepStarted(currentSubStep);
            UpdateUI();
            UpdateProgress();

            Log($"SubStep {currentSubStep.subStepNo}: {currentSubStep.voiceInstruction}");
            return;
        }

        // SubStep 끝 -> Step 완료
        NextStep();
    }

    /// <summary>
    /// 다음 Step으로 진행
    /// </summary>
    private void NextStep()
    {
        eventSystem.StepCompleted(currentStep);

        // 다음 Step이 있으면 진행
        if (currentStepIndex < currentPhase.steps.Count - 1)
        {
            currentStepIndex++;
            currentSubStepIndex = 0;

            currentStep = currentPhase.steps[currentStepIndex];
            currentSubStep = currentStep.subSteps[0];

            eventSystem.StepChanged(currentStep);
            eventSystem.SubStepStarted(currentSubStep);
            UpdateUI();
            UpdateProgress();

            Log($"Step 변경: {currentStep.stepName}");
            return;
        }

        // Step 끝 -> Phase 완료
        NextPhase();
    }

    /// <summary>
    /// 다음 Phase로 진행
    /// </summary>
    private void NextPhase()
    {
        eventSystem.PhaseCompleted(currentPhase);

        // 다음 Phase가 있으면 진행
        if (currentPhaseIndex < currentScenario.phases.Count - 1)
        {
            currentPhaseIndex++;
            currentStepIndex = 0;
            currentSubStepIndex = 0;

            currentPhase = currentScenario.phases[currentPhaseIndex];
            currentStep = currentPhase.steps[0];
            currentSubStep = currentStep.subSteps[0];

            eventSystem.PhaseChanged(currentPhase);
            eventSystem.StepChanged(currentStep);
            eventSystem.SubStepStarted(currentSubStep);
            UpdateUI();
            UpdateProgress();

            Log($"Phase 변경: {currentPhase.phaseName}");
            return;
        }

        // Phase 끝 -> 시나리오 완료
        CompleteScenario();
    }

    /// <summary>
    /// 시나리오 완료
    /// </summary>
    private void CompleteScenario()
    {
        eventSystem.ScenarioCompleted(currentScenario);
        Log($"시나리오 완료: {currentScenario.scenarioName}");
    }

    /// <summary>
    /// UI 업데이트 요청
    /// </summary>
    private void UpdateUI()
    {
        string buttonText = IsLastSubStep && IsLastStep && IsLastPhase ? "완료" : "다음";

        eventSystem.RequestUIUpdate(
            currentScenario.scenarioName,
            currentSubStep.voiceInstruction,
            buttonText
        );
    }

    /// <summary>
    /// 진행도 업데이트 요청
    /// </summary>
    private void UpdateProgress()
    {
        int totalSteps = 0;
        int completedSteps = 0;

        foreach (var phase in currentScenario.phases)
        {
            totalSteps += phase.steps.Count;
        }

        for (int i = 0; i < currentPhaseIndex; i++)
        {
            completedSteps += currentScenario.phases[i].steps.Count;
        }

        completedSteps += currentStepIndex;

        eventSystem.RequestProgressUpdate(completedSteps, totalSteps);
    }

    /// <summary>
    /// 특정 Phase로 이동
    /// </summary>
    public void JumpToPhase(string phaseName)
    {
        int phaseIndex = currentScenario.phases.FindIndex(p => p.phaseName == phaseName);

        if (phaseIndex == -1)
        {
            LogError($"Phase를 찾을 수 없습니다: {phaseName}");
            return;
        }

        currentPhaseIndex = phaseIndex;
        currentStepIndex = 0;
        currentSubStepIndex = 0;

        currentPhase = currentScenario.phases[currentPhaseIndex];
        currentStep = currentPhase.steps[0];
        currentSubStep = currentStep.subSteps[0];

        eventSystem.PhaseChanged(currentPhase);
        eventSystem.StepChanged(currentStep);
        eventSystem.SubStepStarted(currentSubStep);
        UpdateUI();
        UpdateProgress();
    }

    // === 디버그 헬퍼 ===

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log($"[ScenarioManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ScenarioManager] {message}");
    }

    private void OnDestroy()
    {
        eventSystem?.Clear();
    }

    // === Inspector 편집 도우미 ===

    [ContextMenu("📝 빈 시나리오 생성")]
    private void CreateEmptyScenario()
    {
        prototypeScenario = new ScenarioData
        {
            scenarioNo = 1,
            scenarioName = "새 시나리오",
            phases = new List<PhaseData>()
        };

        Debug.Log("빈 시나리오가 생성되었습니다. Inspector에서 편집하세요.");
    }

    [ContextMenu("➕ Phase 추가")]
    private void AddPhase()
    {
        if (prototypeScenario == null)
        {
            Debug.LogError("먼저 시나리오를 생성하세요!");
            return;
        }

        prototypeScenario.phases.Add(new PhaseData
        {
            phaseName = "새 Phase",
            steps = new List<StepData>()
        });

        Debug.Log("Phase가 추가되었습니다.");
    }

    [ContextMenu("📊 시나리오 정보 출력")]
    private void PrintScenarioInfo()
    {
        if (prototypeScenario == null)
        {
            Debug.LogError("시나리오가 없습니다!");
            return;
        }

        Debug.Log($"=== {prototypeScenario.scenarioName} ===");
        Debug.Log($"Phase 수: {prototypeScenario.phases.Count}");

        foreach (var phase in prototypeScenario.phases)
        {
            Debug.Log($"  - {phase.phaseName}: {phase.steps.Count} Steps");

            foreach (var step in phase.steps)
            {
                Debug.Log($"    - {step.stepName}: {step.subSteps.Count} SubSteps");
            }
        }
    }
}