using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject 시나리오 데이터
/// Assets에 저장 가능, 재사용 가능
/// </summary>
[CreateAssetMenu(fileName = "New Scenario", menuName = "Chuna/Scenario Data", order = 1)]
public class ScenarioDataSO : ScriptableObject
{
    [Header("시나리오 정보")]
    public int scenarioNo = 1;
    public string scenarioName = "새 시나리오";
    
    [Header("Phases")]
    public List<PhaseData> phases = new List<PhaseData>();
    
    [ContextMenu("🔧 자동 검증")]
    private void ValidateData()
    {
        Debug.Log($"=== {scenarioName} 검증 ===");
        
        if (phases.Count == 0)
        {
            Debug.LogWarning("Phase가 없습니다!");
            return;
        }
        
        int totalSteps = 0;
        int totalSubSteps = 0;
        
        foreach (var phase in phases)
        {
            if (phase.steps.Count == 0)
            {
                Debug.LogWarning($"Phase '{phase.phaseName}'에 Step이 없습니다!");
            }
            
            totalSteps += phase.steps.Count;
            
            foreach (var step in phase.steps)
            {
                if (step.subSteps.Count == 0)
                {
                    Debug.LogWarning($"Step '{step.stepName}'에 SubStep이 없습니다!");
                }
                
                totalSubSteps += step.subSteps.Count;
            }
        }
        
        Debug.Log($"검증 완료! Phase: {phases.Count}개, Step: {totalSteps}개, SubStep: {totalSubSteps}개");
    }
    
    [ContextMenu("📋 복사본 생성")]
    private void CreateCopy()
    {
        ScenarioDataSO copy = Instantiate(this);
        copy.scenarioName = scenarioName + " (복사본)";
        
#if UNITY_EDITOR
        string path = $"Assets/ScenarioData/{copy.scenarioName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(copy, path);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"복사본 생성: {path}");
#endif
    }
    
    [ContextMenu("➕ 빈 Phase 추가")]
    private void AddEmptyPhase()
    {
        phases.Add(new PhaseData
        {
            phaseName = $"Phase {phases.Count + 1}",
            steps = new List<StepData>()
        });
        
        Debug.Log($"Phase {phases.Count} 추가됨");
        
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    /// <summary>
    /// ScenarioData로 변환
    /// </summary>
    public ScenarioData ToScenarioData()
    {
        return new ScenarioData
        {
            scenarioNo = this.scenarioNo,
            scenarioName = this.scenarioName,
            phases = new List<PhaseData>(this.phases)
        };
    }
}

/// <summary>
/// ScriptableObject를 사용하는 매니저
/// </summary>
public class ScenarioManagerSO : MonoBehaviour
{
    [Header("=== ScriptableObject 시나리오 ===")]
    [Tooltip("사용할 시나리오 에셋")]
    [SerializeField] private ScenarioDataSO scenarioAsset;
    
    [Header("=== 설정 ===")]
    [SerializeField] private bool autoStart = true;
    
    private ScenarioManager coreManager;
    
    private void Start()
    {
        coreManager = GetComponent<ScenarioManager>();
        
        if (coreManager == null)
        {
            Debug.LogError("ScenarioManager 컴포넌트가 필요합니다!");
            return;
        }
        
        if (autoStart && scenarioAsset != null)
        {
            StartScenarioFromAsset();
        }
    }
    
    /// <summary>
    /// ScriptableObject에서 시나리오 시작
    /// </summary>
    public void StartScenarioFromAsset()
    {
        if (scenarioAsset == null)
        {
            Debug.LogError("시나리오 에셋이 설정되지 않았습니다!");
            return;
        }
        
        ScenarioData data = scenarioAsset.ToScenarioData();
        coreManager.StartScenario(data);
    }
    
    /// <summary>
    /// 다른 시나리오 에셋으로 변경
    /// </summary>
    public void ChangeScenario(ScenarioDataSO newScenario)
    {
        scenarioAsset = newScenario;
        StartScenarioFromAsset();
    }
}
