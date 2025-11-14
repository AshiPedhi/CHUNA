using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 시나리오 조건 체크 인터페이스
/// 사용자가 직접 조건을 구현할 수 있도록 제공
/// </summary>
public interface IScenarioCondition
{
    bool IsConditionMet();
    string GetConditionDescription();
}

/// <summary>
/// 시나리오 조건 관리자
/// ✅ CSV 데이터만으로 완전 자동화
/// - duration > 0: 자동 시간 대기 후 진행
/// - handTrackingFileName 있음: 자동 손 동작 조건 등록 (ScenarioActionHandler에서)
/// - 둘 다 없음: 토글로 수동 진행
/// </summary>
public class ScenarioConditionManager : MonoBehaviour
{
    [Header("=== 조건 체크 설정 ===")]
    [Tooltip("조건 체크 간격 (초)")]
    [SerializeField] private float checkInterval = 0.5f;

    [Tooltip("완료 후 다음 단계까지 딜레이 (초)")]
    [SerializeField] private float completionDelay = 2f;

    [Header("=== 완료 알림 UI ===")]
    [SerializeField] private GameObject completionAlertPanel;
    [SerializeField] private TMPro.TextMeshProUGUI completionAlertText;

    [Header("=== 사운드 (선택사항) ===")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip completionSound;

    // 현재 조건
    private IScenarioCondition currentCondition;
    private bool isCheckingCondition = false;
    private Coroutine checkCoroutine;

    // 이벤트 시스템
    private ScenarioEventSystem eventSystem;
    private ScenarioManager scenarioManager;

    // 조건 레지스트리 (SubStep별로 조건을 등록)
    private Dictionary<string, IScenarioCondition> conditionRegistry = new Dictionary<string, IScenarioCondition>();

    void Awake()
    {
        eventSystem = ScenarioEventSystem.Instance;
        scenarioManager = FindObjectOfType<ScenarioManager>();

        // 완료 알림 패널 초기화
        if (completionAlertPanel != null)
        {
            completionAlertPanel.SetActive(false);
        }
    }

    void OnEnable()
    {
        // 이벤트 구독
        eventSystem.OnSubStepStarted += OnSubStepStarted;
    }

    void OnDisable()
    {
        // 이벤트 구독 해제
        eventSystem.OnSubStepStarted -= OnSubStepStarted;

        // 진행 중인 체크 중단
        StopConditionCheck();
    }

    /// <summary>
    /// SubStep 시작 시 호출 - CSV 데이터 기반 자동 조건 처리
    /// ✅ 수동 등록 불필요! CSV만 작성하면 자동으로 처리됨
    /// </summary>
    private void OnSubStepStarted(SubStepData subStep)
    {
        // 가이드 스텝은 자동으로 진행 (조건 체크 안 함)
        if (scenarioManager.CurrentStep.IsGuideStep())
        {
            Debug.Log("[ConditionManager] 가이드 스텝 - 조건 체크 없음");
            StopConditionCheck();
            return;
        }

        // 해당 SubStep의 조건 찾기
        string conditionKey = GetConditionKey(subStep);

        if (conditionRegistry.ContainsKey(conditionKey))
        {
            // ✅ 조건이 등록된 경우: 조건 체크 시작
            // (ScenarioActionHandler가 handTrackingFileName을 보고 자동으로 HandPose 조건을 등록함)
            currentCondition = conditionRegistry[conditionKey];
            StartConditionCheck();
        }
        else if (subStep.duration > 0)
        {
            // ✅ CSV의 duration > 0: 완료 알림 없이 duration 후 자동 진행
            Debug.Log($"[ConditionManager] CSV duration={subStep.duration}초 감지 - 자동 진행 (완료 알림 없음)");
            currentCondition = null;
            StopConditionCheck();
            StartCoroutine(AutoProgressWithoutAlert(subStep.duration));
        }
        else
        {
            // ✅ 조건도 없고 duration도 0: 토글로 수동 진행
            Debug.Log($"[ConditionManager] 조건 및 duration 없음 - 토글로 수동 진행");
            currentCondition = null;
            StopConditionCheck();
        }
    }

    /// <summary>
    /// 조건 체크 시작
    /// </summary>
    private void StartConditionCheck()
    {
        StopConditionCheck();

        isCheckingCondition = true;
        checkCoroutine = StartCoroutine(ConditionCheckRoutine());

        Debug.Log($"[ConditionManager] 조건 체크 시작: {currentCondition?.GetConditionDescription()}");
    }

    /// <summary>
    /// 조건 체크 중단
    /// </summary>
    private void StopConditionCheck()
    {
        isCheckingCondition = false;

        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }
    }

    /// <summary>
    /// 조건 체크 루틴
    /// </summary>
    private IEnumerator ConditionCheckRoutine()
    {
        while (isCheckingCondition && currentCondition != null)
        {
            // 조건 확인
            if (currentCondition.IsConditionMet())
            {
                Debug.Log($"[ConditionManager] 조건 만족: {currentCondition.GetConditionDescription()}");

                // 체크 중단
                isCheckingCondition = false;

                // 완료 처리
                yield return StartCoroutine(OnConditionCompleted());

                yield break;
            }

            // 다음 체크까지 대기
            yield return new WaitForSeconds(checkInterval);
        }
    }

    /// <summary>
    /// 조건 완료 시 처리 (완료 알림 + 딜레이)
    /// HandPose 조건 등 등록된 조건에서 사용
    /// </summary>
    private IEnumerator OnConditionCompleted()
    {
        // 완료 알림 표시
        ShowCompletionAlert();

        // 완료 사운드 재생
        PlayCompletionSound();

        // 딜레이
        yield return new WaitForSeconds(completionDelay);

        // 완료 알림 숨김
        HideCompletionAlert();

        // 다음 SubStep으로 진행
        if (scenarioManager != null)
        {
            scenarioManager.NextSubStep();
        }
    }

    /// <summary>
    /// 완료 알림 없이 자동 진행 (CSV duration 전용)
    /// </summary>
    private IEnumerator AutoProgressWithoutAlert(int duration)
    {
        // duration만큼 대기
        yield return new WaitForSeconds(duration);

        // 완료 알림 없이 바로 다음 SubStep으로 진행
        if (scenarioManager != null)
        {
            Debug.Log($"[ConditionManager] {duration}초 경과 - 다음 단계로 자동 진행");
            scenarioManager.NextSubStep();
        }
    }

    /// <summary>
    /// 완료 알림 표시
    /// </summary>
    private void ShowCompletionAlert()
    {
        if (completionAlertPanel != null)
        {
            completionAlertPanel.SetActive(true);
        }

        if (completionAlertText != null)
        {
            completionAlertText.text = "✓ 완료!";
        }

        Debug.Log("[ConditionManager] 완료 알림 표시");
    }

    /// <summary>
    /// 완료 알림 숨김
    /// </summary>
    private void HideCompletionAlert()
    {
        if (completionAlertPanel != null)
        {
            completionAlertPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 완료 사운드 재생
    /// </summary>
    private void PlayCompletionSound()
    {
        if (audioSource != null && completionSound != null)
        {
            audioSource.PlayOneShot(completionSound);
        }
    }

    /// <summary>
    /// 조건 키 생성
    /// </summary>
    private string GetConditionKey(SubStepData subStep)
    {
        // Phase_Step_SubStep 형식으로 키 생성
        string phaseName = scenarioManager.CurrentPhase.phaseName;
        string stepName = scenarioManager.CurrentStep.stepName;
        int subStepNo = subStep.subStepNo;

        return $"{phaseName}_{stepName}_{subStepNo}";
    }

    // ========== Public API ==========

    /// <summary>
    /// 조건 등록
    /// ✅ ScenarioActionHandler가 handTrackingFileName을 감지하면 자동으로 호출함
    /// 수동 등록도 가능 (특수한 경우에만)
    /// </summary>
    public void RegisterCondition(string phaseName, string stepName, int subStepNo, IScenarioCondition condition)
    {
        string key = $"{phaseName}_{stepName}_{subStepNo}";

        if (conditionRegistry.ContainsKey(key))
        {
            Debug.LogWarning($"[ConditionManager] 조건이 이미 등록되어 있습니다: {key}");
            conditionRegistry[key] = condition;
        }
        else
        {
            conditionRegistry.Add(key, condition);
        }

        Debug.Log($"[ConditionManager] 조건 등록: {key} - {condition.GetConditionDescription()}");
    }

    /// <summary>
    /// 조건 등록 해제
    /// </summary>
    public void UnregisterCondition(string phaseName, string stepName, int subStepNo)
    {
        string key = $"{phaseName}_{stepName}_{subStepNo}";

        if (conditionRegistry.ContainsKey(key))
        {
            conditionRegistry.Remove(key);
            Debug.Log($"[ConditionManager] 조건 등록 해제: {key}");
        }
    }

    /// <summary>
    /// 모든 조건 등록 해제
    /// </summary>
    public void ClearAllConditions()
    {
        conditionRegistry.Clear();
        Debug.Log("[ConditionManager] 모든 조건 등록 해제");
    }

    /// <summary>
    /// 수동으로 현재 단계 완료 처리
    /// </summary>
    public void CompleteCurrentStep()
    {
        if (isCheckingCondition)
        {
            StopConditionCheck();
            StartCoroutine(OnConditionCompleted());
        }
    }

    /// <summary>
    /// 조건 체크 활성화 여부
    /// </summary>
    public bool IsCheckingCondition => isCheckingCondition;
}

// ========== 조건 클래스들 ==========

/// <summary>
/// 시간 기반 조건 (N초 경과 시 완료)
/// 참고: CSV의 duration은 자동으로 처리되므로 수동 등록 시에만 사용
/// </summary>
public class TimeBasedCondition : IScenarioCondition
{
    private float startTime;
    private float requiredDuration;

    public TimeBasedCondition(float duration)
    {
        requiredDuration = duration;
        startTime = Time.time;
    }

    public bool IsConditionMet()
    {
        return Time.time - startTime >= requiredDuration;
    }

    public string GetConditionDescription()
    {
        return $"{requiredDuration}초 대기";
    }
}

/// <summary>
/// 버튼 클릭 조건
/// </summary>
public class ButtonClickCondition : IScenarioCondition
{
    private bool isClicked = false;

    public void OnButtonClick()
    {
        isClicked = true;
    }

    public bool IsConditionMet()
    {
        return isClicked;
    }

    public string GetConditionDescription()
    {
        return "버튼 클릭 대기";
    }

    public void Reset()
    {
        isClicked = false;
    }
}

/// <summary>
/// 위치 기반 조건 (특정 위치에 도달 시 완료)
/// </summary>
public class PositionBasedCondition : IScenarioCondition
{
    private Transform targetTransform;
    private Vector3 targetPosition;
    private float threshold;

    public PositionBasedCondition(Transform target, Vector3 position, float distanceThreshold = 0.1f)
    {
        targetTransform = target;
        targetPosition = position;
        threshold = distanceThreshold;
    }

    public bool IsConditionMet()
    {
        if (targetTransform == null) return false;

        float distance = Vector3.Distance(targetTransform.position, targetPosition);
        return distance <= threshold;
    }

    public string GetConditionDescription()
    {
        return $"목표 위치 도달 (거리: {threshold}m 이내)";
    }
}

/// <summary>
/// 커스텀 델리게이트 조건
/// </summary>
public class CustomCondition : IScenarioCondition
{
    private Func<bool> conditionFunc;
    private string description;

    public CustomCondition(Func<bool> condition, string desc = "커스텀 조건")
    {
        conditionFunc = condition;
        description = desc;
    }

    public bool IsConditionMet()
    {
        return conditionFunc != null && conditionFunc();
    }

    public string GetConditionDescription()
    {
        return description;
    }
}

/// <summary>
/// 손 동작 트래킹 조건 (HandPosePlayer 연동)
/// ✅ ScenarioActionHandler가 CSV의 handTrackingFileName을 감지하여 자동 생성 및 등록
/// </summary>
public class HandPoseCondition : IScenarioCondition
{
    private HandPosePlayer handPosePlayer;
    private bool isCompleted = false;
    private string fileName;

    public HandPoseCondition(HandPosePlayer player, string trackingFileName)
    {
        handPosePlayer = player;
        fileName = trackingFileName;

        if (player != null)
        {
            // HandPosePlayer의 완료 이벤트 구독 (리플렉션 사용)
            try
            {
                var eventInfo = player.GetType().GetEvent("OnSequenceCompleted");
                if (eventInfo != null)
                {
                    var handler = new System.Action(OnSequenceCompleted);
                    eventInfo.AddEventHandler(player, handler);
                    Debug.Log($"<color=cyan>[HandPoseCondition] 이벤트 구독 성공: {trackingFileName}</color>");
                }
                else
                {
                    Debug.LogWarning("[HandPoseCondition] HandPosePlayer에 OnSequenceCompleted 이벤트가 없습니다.\n" +
                                   "HandPosePlayer.cs 수정이 필요합니다. 출력된 HandPosePlayer.cs 파일을 확인하세요.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HandPoseCondition] 이벤트 구독 실패: {e.Message}");
            }
        }
    }

    private void OnSequenceCompleted()
    {
        isCompleted = true;
        Debug.Log($"<color=green>[HandPoseCondition] 손 동작 완료: {fileName}</color>");
    }

    public bool IsConditionMet()
    {
        return isCompleted;
    }

    public string GetConditionDescription()
    {
        return $"손 동작 트래킹: {fileName}";
    }

    public void Reset()
    {
        isCompleted = false;
    }
}