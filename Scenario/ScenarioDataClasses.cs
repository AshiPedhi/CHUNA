using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SubStep 데이터 (Inspector 편집 가능)
/// </summary>
[Serializable]
public class SubStepData
{
    [Header("SubStep 정보")]
    [Tooltip("SubStep 번호")]
    public int subStepNo;

    [Tooltip("소요 시간 (초) - 0이면 무제한")]
    public int duration;

    [Header("안내 내용")]
    [Tooltip("화면에 표시될 텍스트 (선택사항)")]
    [TextArea(2, 4)]
    public string textInstruction;

    [Tooltip("음성으로 안내될 내용")]
    [TextArea(3, 6)]
    public string voiceInstruction;

    [Header("동작 설정")]
    [Tooltip("'상동' 체크 시 이전 SubStep과 동일")]
    public bool isSameToPrevious;

    [Tooltip("이 SubStep에서 실행할 특별한 액션 (선택사항)")]
    public string customAction;

    [Header("환자 움직임 데이터 (선택사항)")]
    [Tooltip("환자 움직임 CSV 파일명 (확장자 제외) - 비어있으면 환자 움직임 없음")]
    public string patientMotionFileName;

    [Header("핸드 트래킹 데이터 (선택사항)")]
    [Tooltip("HandPosePlayer가 로드할 CSV 파일명 (확장자 제외) - 비어있으면 재생 없음")]
    public string handTrackingFileName;
}

/// <summary>
/// Step 데이터 (Inspector 편집 가능)
/// </summary>
[Serializable]
public class StepData
{
    [Header("Step 정보")]
    [Tooltip("Step 번호 (0=가이드, 1~5=실제 단계)")]
    public int stepNo;

    [Tooltip("Step 이름 (예: 가이드, 평가, 세판상박회인)")]
    public string stepName;

    [Header("SubSteps")]
    [Tooltip("이 Step에 포함된 SubStep 목록")]
    public List<SubStepData> subSteps = new List<SubStepData>();

    /// <summary>
    /// 가이드 Step인지 확인
    /// </summary>
    public bool IsGuideStep() => stepNo == 0;
}

/// <summary>
/// Phase 데이터 (Inspector 편집 가능)
/// </summary>
[Serializable]
public class PhaseData
{
    [Header("Phase 정보")]
    [Tooltip("Phase 이름 (예: 평가, 중부, 전부, 후부)")]
    public string phaseName;

    [Header("Steps")]
    [Tooltip("이 Phase에 포함된 Step 목록")]
    public List<StepData> steps = new List<StepData>();
}

/// <summary>
/// 시나리오 데이터 (Inspector 편집 가능)
/// </summary>
[Serializable]
public class ScenarioData
{
    [Header("시나리오 정보")]
    [Tooltip("시나리오 번호")]
    public int scenarioNo;

    [Tooltip("시나리오 이름 (예: 상부승모근)")]
    public string scenarioName;

    [Header("Phases")]
    [Tooltip("이 시나리오에 포함된 Phase 목록")]
    public List<PhaseData> phases = new List<PhaseData>();
}

/// <summary>
/// 시나리오 컬렉션 (여러 시나리오 관리)
/// </summary>
[Serializable]
public class ScenarioCollection
{
    public List<ScenarioData> scenarios = new List<ScenarioData>();
}