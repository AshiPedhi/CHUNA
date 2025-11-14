#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// ScenarioDataSO용 커스텀 에디터
/// Inspector에서 더 편하게 편집할 수 있도록 개선
/// </summary>
[CustomEditor(typeof(ScenarioDataSO))]
public class ScenarioDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ScenarioDataSO scenario = (ScenarioDataSO)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("시나리오 빠른 편집", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("➕ Phase 추가", GUILayout.Height(30)))
        {
            Undo.RecordObject(scenario, "Add Phase");
            scenario.phases.Add(new PhaseData
            {
                phaseName = $"Phase {scenario.phases.Count + 1}",
                steps = new System.Collections.Generic.List<StepData>()
            });
            EditorUtility.SetDirty(scenario);
        }
        
        if (GUILayout.Button("🔧 검증", GUILayout.Height(30)))
        {
            ValidateScenario(scenario);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // 기본 Inspector 표시
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        // 통계 표시
        ShowStatistics(scenario);
    }
    
    private void ValidateScenario(ScenarioDataSO scenario)
    {
        int phaseCount = scenario.phases.Count;
        int stepCount = 0;
        int subStepCount = 0;
        
        foreach (var phase in scenario.phases)
        {
            stepCount += phase.steps.Count;
            foreach (var step in phase.steps)
            {
                subStepCount += step.subSteps.Count;
            }
        }
        
        if (phaseCount == 0)
        {
            EditorUtility.DisplayDialog("검증 실패", "Phase가 없습니다!", "확인");
            return;
        }
        
        if (stepCount == 0)
        {
            EditorUtility.DisplayDialog("검증 실패", "Step이 없습니다!", "확인");
            return;
        }
        
        if (subStepCount == 0)
        {
            EditorUtility.DisplayDialog("검증 실패", "SubStep이 없습니다!", "확인");
            return;
        }
        
        EditorUtility.DisplayDialog("검증 성공", 
            $"시나리오가 정상입니다!\n\n" +
            $"Phase: {phaseCount}개\n" +
            $"Step: {stepCount}개\n" +
            $"SubStep: {subStepCount}개", "확인");
    }
    
    private void ShowStatistics(ScenarioDataSO scenario)
    {
        EditorGUILayout.LabelField("통계", EditorStyles.boldLabel);
        
        int totalSteps = 0;
        int totalSubSteps = 0;
        
        foreach (var phase in scenario.phases)
        {
            totalSteps += phase.steps.Count;
            foreach (var step in phase.steps)
            {
                totalSubSteps += step.subSteps.Count;
            }
        }
        
        EditorGUILayout.LabelField($"Phase: {scenario.phases.Count}개");
        EditorGUILayout.LabelField($"Step: {totalSteps}개");
        EditorGUILayout.LabelField($"SubStep: {totalSubSteps}개");
    }
}
#endif
