using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Oculus.Interaction.Input;
using Oculus.Interaction;
using System.Globalization;

public class HandPosePlayer : MonoBehaviour
{
    [SerializeField]
    private HandVisual leftHandVisual;

    [SerializeField]
    private HandVisual rightHandVisual;

    [SerializeField]
    private HandVisual playerLeftHand;

    [SerializeField]
    private HandVisual playerRightHand;

    [SerializeField]
    private float playbackInterval = 0.1f;

    [Header("동작 비교 설정")]
    [SerializeField]
    private float positionThreshold = 0.05f;

    [SerializeField]
    private float rotationThreshold = 15f;

    [SerializeField]
    private float similarityPercentage = 0.7f;

    [Header("손 전체 위치 비교")]
    [SerializeField]
    private bool compareHandPosition = true;

    [SerializeField]
    private float handPositionThreshold = 0.1f;

    [SerializeField]
    private bool compareHandRotation = true;

    [SerializeField]
    private float handRotationThreshold = 20f;

    [SerializeField]
    private Transform referencePoint;

    [Header("리플레이 손 표시")]
    [SerializeField]
    private bool showReplayHands = true;

    [SerializeField]
    private Material replayHandMaterial;

    [SerializeField]
    private float replayHandAlpha = 0.5f;

    [SerializeField]
    private Color replayHandColor = new Color(0.3f, 0.5f, 1f, 0.5f);  // 파란색

    [Header("독립 재생 설정")]
    [SerializeField]
    private bool playLeftHand = true;

    [SerializeField]
    private bool playRightHand = true;

    [SerializeField]
    private bool compareLeftHand = true;

    [SerializeField]
    private bool compareRightHand = true;

    [SerializeField]
    private bool enableAutoProgress = true;

    [SerializeField]
    private List<HandJointId> keyJoints = new List<HandJointId>()
    {
        HandJointId.HandWristRoot,
        HandJointId.HandThumb3,
        HandJointId.HandIndex3,
        HandJointId.HandMiddle3,
        HandJointId.HandRing3,
        HandJointId.HandPinky3
    };

    [Header("디버그 표시")]
    [SerializeField]
    private bool showDebugGizmos = true;

    [SerializeField]
    private bool showPositionLines = true;

    [SerializeField]
    private float gizmoSphereSize = 0.02f;

    [Header("시나리오 연동")]
    [SerializeField]
    private bool integrateWithScenario = true;

    // 완료 이벤트
    public event System.Action OnSequenceCompleted;
    public event System.Action OnLeftHandCompleted;
    public event System.Action OnRightHandCompleted;

    private bool hasNotifiedCompletion = false;

    private List<PoseFrame> loadedSequence = new List<PoseFrame>();
    private bool isLeftPlaying = false;
    private bool isRightPlaying = false;
    private int currentLeftPlaybackIndex = 0;
    private int currentRightPlaybackIndex = 0;
    private float lastLeftPlaybackTime = 0f;
    private float lastRightPlaybackTime = 0f;

    private float currentLeftSimilarity = 0f;
    private float currentRightSimilarity = 0f;
    private bool isLeftHandSimilar = false;
    private bool isRightHandSimilar = false;

    private float leftHandPositionError = 0f;
    private float rightHandPositionError = 0f;
    private float leftHandRotationError = 0f;
    private float rightHandRotationError = 0f;

    // 디버그용 위치 저장
    private Vector3 leftReplayTargetPosition;
    private Vector3 rightReplayTargetPosition;
    private Vector3 leftPlayerCurrentPosition;
    private Vector3 rightPlayerCurrentPosition;

    [System.Serializable]
    private class PoseFrame
    {
        public Dictionary<int, PoseData> leftLocalPoses = new Dictionary<int, PoseData>();
        public Dictionary<int, PoseData> rightLocalPoses = new Dictionary<int, PoseData>();
        public Vector3 leftHandWorldPosition;
        public Quaternion leftHandWorldRotation;
        public Vector3 rightHandWorldPosition;
        public Quaternion rightHandWorldRotation;
        public float timestamp;
    }

    [System.Serializable]
    private class PoseData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public struct SimilarityResult
    {
        public float leftHandSimilarity;
        public float rightHandSimilarity;
        public bool leftHandPassed;
        public bool rightHandPassed;
        public bool overallPassed;
        public float leftHandPositionError;
        public float rightHandPositionError;
        public float leftHandRotationError;
        public float rightHandRotationError;
        public bool leftHandPositionPassed;
        public bool rightHandPositionPassed;
    }

    void Start()
    {
        InitializeReplayHands();

        if (referencePoint == null)
        {
            Debug.LogWarning("기준점이 없어 월드 좌표로 비교합니다.");
        }
        else
        {
            Debug.Log($"기준점 설정됨: {referencePoint.name} at {referencePoint.position}");
        }
    }

    private void InitializeReplayHands()
    {
        if (leftHandVisual != null)
        {
            leftHandVisual.enabled = true;  // enabled를 true로 변경
            SetupReplayHandVisual(leftHandVisual, "왼손");
        }

        if (rightHandVisual != null)
        {
            rightHandVisual.enabled = true;  // enabled를 true로 변경
            SetupReplayHandVisual(rightHandVisual, "오른손");
        }

        SetReplayHandsVisible(showReplayHands);
    }

    private void SetupReplayHandVisual(HandVisual handVisual, string handName)
    {
        if (handVisual == null) return;

        SkinnedMeshRenderer[] renderers = handVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        Debug.Log($"{handName} 리플레이 설정: {renderers.Length}개 렌더러 발견");

        foreach (var renderer in renderers)
        {
            Material mat;

            if (replayHandMaterial != null)
            {
                mat = new Material(replayHandMaterial);
            }
            else
            {
                mat = new Material(renderer.material);

                // Transparent 모드로 변경
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            // 색상 및 투명도 적용
            if (mat.HasProperty("_Color"))
            {
                Color finalColor = replayHandColor;
                finalColor.a = replayHandAlpha;
                mat.color = finalColor;
            }

            renderer.material = mat;
            renderer.enabled = showReplayHands;
        }
    }

    void Update()
    {
        if (loadedSequence.Count == 0)
            return;

        if (isLeftPlaying && playLeftHand && currentLeftPlaybackIndex < loadedSequence.Count)
        {
            UpdateLeftHand();

            // 시나리오 통합: 양손 완료 체크
            if (integrateWithScenario && !hasNotifiedCompletion)
            {
                CheckSequenceCompletion();
            }
        }

        if (isRightPlaying && playRightHand && currentRightPlaybackIndex < loadedSequence.Count)
        {
            UpdateRightHand();
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying)
            return;

        // 기준점 표시
        if (referencePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(referencePoint.position, 0.05f);
            Gizmos.DrawLine(referencePoint.position, referencePoint.position + Vector3.up * 0.1f);
        }

        // 왼손 위치 표시
        if (isLeftPlaying && showPositionLines)
        {
            // 목표 위치 (리플레이)
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(leftReplayTargetPosition, gizmoSphereSize);

            // 현재 위치 (플레이어)
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(leftPlayerCurrentPosition, gizmoSphereSize);

            // 연결선
            Gizmos.color = leftHandPositionError <= handPositionThreshold ? Color.green : Color.red;
            Gizmos.DrawLine(leftReplayTargetPosition, leftPlayerCurrentPosition);

            // 오차 텍스트 (에디터에서만)
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                (leftReplayTargetPosition + leftPlayerCurrentPosition) / 2f + Vector3.up * 0.05f,
                $"왼손 오차: {leftHandPositionError * 100f:F1}cm"
            );
#endif
        }

        // 오른손 위치 표시
        if (isRightPlaying && showPositionLines)
        {
            // 목표 위치 (리플레이)
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(rightReplayTargetPosition, gizmoSphereSize);

            // 현재 위치 (플레이어)
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(rightPlayerCurrentPosition, gizmoSphereSize);

            // 연결선
            Gizmos.color = rightHandPositionError <= handPositionThreshold ? Color.green : Color.red;
            Gizmos.DrawLine(rightReplayTargetPosition, rightPlayerCurrentPosition);

            // 오차 텍스트
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                (rightReplayTargetPosition + rightPlayerCurrentPosition) / 2f + Vector3.up * 0.05f,
                $"오른손 오차: {rightHandPositionError * 100f:F1}cm"
            );
#endif
        }
    }

    private void UpdateLeftHand()
    {
        if (enableAutoProgress)
        {
            if (compareLeftHand)
            {
                var result = CompareLeftPose();

                bool passed = result.leftHandPassed;
                if (compareHandPosition)
                {
                    passed = passed && result.leftHandPositionPassed;
                }

                if (passed)
                {
                    Debug.Log($"<color=green>왼손 프레임 {currentLeftPlaybackIndex} 통과!</color>\n" +
                             $"조인트 유사도: {result.leftHandSimilarity * 100:F1}%\n" +
                             $"위치 오차: {result.leftHandPositionError * 100:F1}cm\n" +
                             $"회전 오차: {result.leftHandRotationError:F1}°");

                    ProgressLeftHand();
                }
            }
            else
            {
                if (Time.time - lastLeftPlaybackTime >= playbackInterval)
                {
                    ProgressLeftHand();
                    lastLeftPlaybackTime = Time.time;
                }
            }
        }
        else
        {
            if (Time.time - lastLeftPlaybackTime >= playbackInterval)
            {
                ProgressLeftHand();
                lastLeftPlaybackTime = Time.time;
            }
        }
    }

    private void UpdateRightHand()
    {
        if (enableAutoProgress)
        {
            if (compareRightHand)
            {
                var result = CompareRightPose();

                bool passed = result.rightHandPassed;
                if (compareHandPosition)
                {
                    passed = passed && result.rightHandPositionPassed;
                }

                if (passed)
                {
                    Debug.Log($"<color=green>오른손 프레임 {currentRightPlaybackIndex} 통과!</color>\n" +
                             $"조인트 유사도: {result.rightHandSimilarity * 100:F1}%\n" +
                             $"위치 오차: {result.rightHandPositionError * 100:F1}cm\n" +
                             $"회전 오차: {result.rightHandRotationError:F1}°");

                    ProgressRightHand();
                }
            }
            else
            {
                if (Time.time - lastRightPlaybackTime >= playbackInterval)
                {
                    ProgressRightHand();
                    lastRightPlaybackTime = Time.time;
                }
            }
        }
        else
        {
            if (Time.time - lastRightPlaybackTime >= playbackInterval)
            {
                ProgressRightHand();
                lastRightPlaybackTime = Time.time;
            }
        }
    }

    private void ProgressLeftHand()
    {
        currentLeftPlaybackIndex++;

        if (currentLeftPlaybackIndex >= loadedSequence.Count)
        {
            isLeftPlaying = false;

            if (integrateWithScenario)
            {
                OnLeftHandCompleted?.Invoke();
            }
            Debug.Log("<color=cyan>왼손 재생 완료!</color>");
            return;
        }

        ApplyLeftHandFrame();
    }

    private void ProgressRightHand()
    {
        currentRightPlaybackIndex++;

        if (currentRightPlaybackIndex >= loadedSequence.Count)
        {
            isRightPlaying = false;

            if (integrateWithScenario)
            {
                OnRightHandCompleted?.Invoke();
            }
            Debug.Log("<color=cyan>오른손 재생 완료!</color>");
            return;
        }

        ApplyRightHandFrame();
    }

    private SimilarityResult CompareLeftPose()
    {
        SimilarityResult result = new SimilarityResult();

        if (playerLeftHand == null || currentLeftPlaybackIndex >= loadedSequence.Count)
            return result;

        PoseFrame currentFrame = loadedSequence[currentLeftPlaybackIndex];

        bool passed;
        result.leftHandSimilarity = ComparePose(playerLeftHand, currentFrame.leftLocalPoses, out passed, "왼손");
        result.leftHandPassed = passed;

        if (compareHandPosition)
        {
            CompareHandWorldPosition(
                playerLeftHand,
                currentFrame.leftHandWorldPosition,
                currentFrame.leftHandWorldRotation,
                out result.leftHandPositionError,
                out result.leftHandRotationError,
                out result.leftHandPositionPassed,
                "왼손"
            );
        }
        else
        {
            result.leftHandPositionPassed = true;
        }

        isLeftHandSimilar = result.leftHandPassed && result.leftHandPositionPassed;
        currentLeftSimilarity = result.leftHandSimilarity;
        leftHandPositionError = result.leftHandPositionError;
        leftHandRotationError = result.leftHandRotationError;

        return result;
    }

    private SimilarityResult CompareRightPose()
    {
        SimilarityResult result = new SimilarityResult();

        if (playerRightHand == null || currentRightPlaybackIndex >= loadedSequence.Count)
            return result;

        PoseFrame currentFrame = loadedSequence[currentRightPlaybackIndex];

        bool passed;
        result.rightHandSimilarity = ComparePose(playerRightHand, currentFrame.rightLocalPoses, out passed, "오른손");
        result.rightHandPassed = passed;

        if (compareHandPosition)
        {
            CompareHandWorldPosition(
                playerRightHand,
                currentFrame.rightHandWorldPosition,
                currentFrame.rightHandWorldRotation,
                out result.rightHandPositionError,
                out result.rightHandRotationError,
                out result.rightHandPositionPassed,
                "오른손"
            );
        }
        else
        {
            result.rightHandPositionPassed = true;
        }

        isRightHandSimilar = result.rightHandPassed && result.rightHandPositionPassed;
        currentRightSimilarity = result.rightHandSimilarity;
        rightHandPositionError = result.rightHandPositionError;
        rightHandRotationError = result.rightHandRotationError;

        return result;
    }

    private void CompareHandWorldPosition(
        HandVisual playerHand,
        Vector3 targetWorldPosition,
        Quaternion targetWorldRotation,
        out float positionError,
        out float rotationError,
        out bool passed,
        string handName)
    {
        positionError = 0f;
        rotationError = 0f;
        passed = false;

        if (playerHand == null || playerHand.Hand == null || !playerHand.Hand.IsTrackedDataValid)
            return;

        Transform wrist = playerHand.Joints[(int)HandJointId.HandWristRoot];
        if (wrist == null)
            return;

        Vector3 targetPos = targetWorldPosition;

        // 기준점이 있으면 기준점 기준으로 절대 위치 계산
        if (referencePoint != null)
        {
            targetPos = referencePoint.position + targetWorldPosition;
        }

        Vector3 playerPos = wrist.position;

        // 디버그용 위치 저장
        if (handName == "왼손")
        {
            leftReplayTargetPosition = targetPos;
            leftPlayerCurrentPosition = playerPos;
        }
        else
        {
            rightReplayTargetPosition = targetPos;
            rightPlayerCurrentPosition = playerPos;
        }

        positionError = Vector3.Distance(playerPos, targetPos);

        if (compareHandRotation)
        {
            rotationError = Quaternion.Angle(wrist.rotation, targetWorldRotation);
        }

        bool positionPassed = positionError <= handPositionThreshold;
        bool rotationPassed = !compareHandRotation || rotationError <= handRotationThreshold;
        passed = positionPassed && rotationPassed;

#if UNITY_EDITOR
        if (Time.frameCount % 30 == 0)
        {
            string posStatus = positionPassed ? "<color=green>OK</color>" : "<color=red>NG</color>";
            string rotStatus = rotationPassed ? "<color=green>OK</color>" : "<color=red>NG</color>";

            Debug.Log($"{handName} 위치 {posStatus}: {positionError * 100:F1}cm " +
                     $"(임계값: {handPositionThreshold * 100:F1}cm)\n" +
                     $"목표: {targetPos}, 현재: {playerPos}\n" +
                     $"{handName} 회전 {rotStatus}: {rotationError:F1}° " +
                     $"(임계값: {handRotationThreshold:F1}°)");
        }
#endif
    }

    private float ComparePose(HandVisual playerHand, Dictionary<int, PoseData> replayPoses, out bool passed, string handName)
    {
        passed = false;

        if (playerHand == null || playerHand.Hand == null || !playerHand.Hand.IsTrackedDataValid)
        {
            return 0f;
        }

        int similarJointCount = 0;
        int totalJointCount = 0;

        foreach (HandJointId jointId in keyJoints)
        {
            int jointIndex = (int)jointId;

            if (!replayPoses.ContainsKey(jointIndex))
                continue;

            totalJointCount++;

            if (jointIndex >= playerHand.Joints.Count || playerHand.Joints[jointIndex] == null)
                continue;

            Transform playerJoint = playerHand.Joints[jointIndex];
            PoseData replayPose = replayPoses[jointIndex];

            float positionDistance = Vector3.Distance(playerJoint.localPosition, replayPose.position);
            float rotationAngle = Quaternion.Angle(playerJoint.localRotation, replayPose.rotation);

            if (positionDistance <= positionThreshold && rotationAngle <= rotationThreshold)
            {
                similarJointCount++;
            }
        }

        if (totalJointCount == 0)
            return 0f;

        float similarity = (float)similarJointCount / totalJointCount;
        passed = similarity >= similarityPercentage;

        return similarity;
    }

    public void StartPlaybackFromCSV(string csvFileName)
    {
        ResetCompletionFlag();  // 재생 시작 시 완료 플래그 초기화

        string path = Path.Combine(Application.persistentDataPath, csvFileName + ".csv");
        if (!File.Exists(path))
        {
            Debug.LogError("CSV 파일 없음: " + path);
            return;
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            Debug.LogError("CSV 데이터 부족.");
            return;
        }

        loadedSequence.Clear();
        PoseFrame currentFrame = null;
        int lastFrameIndex = -1;

        CultureInfo invariantCulture = CultureInfo.InvariantCulture;

        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                string[] values = lines[i].Split(',');

                if (values.Length < 11)
                {
                    Debug.LogWarning($"라인 {i}: 필드가 부족합니다. ({values.Length}개)");
                    continue;
                }

                int frameIndex = int.Parse(values[0], invariantCulture);
                string handType = values[1].Trim();
                int jointId = int.Parse(values[2], invariantCulture);

                Vector3 pos = new Vector3(
                    float.Parse(values[3], invariantCulture),
                    float.Parse(values[4], invariantCulture),
                    float.Parse(values[5], invariantCulture)
                );
                Quaternion rot = new Quaternion(
                    float.Parse(values[6], invariantCulture),
                    float.Parse(values[7], invariantCulture),
                    float.Parse(values[8], invariantCulture),
                    float.Parse(values[9], invariantCulture)
                );
                float timestamp = float.Parse(values[10], invariantCulture);

                if (frameIndex != lastFrameIndex)
                {
                    if (currentFrame != null) loadedSequence.Add(currentFrame);
                    currentFrame = new PoseFrame { timestamp = timestamp };
                    lastFrameIndex = frameIndex;
                }

                PoseData poseData = new PoseData { position = pos, rotation = rot };

                if (handType == "Left")
                {
                    currentFrame.leftLocalPoses[jointId] = poseData;

                    if (jointId == 0 && values.Length >= 18)
                    {
                        if (!string.IsNullOrEmpty(values[11]) &&
                            !string.IsNullOrEmpty(values[14]))
                        {
                            currentFrame.leftHandWorldPosition = new Vector3(
                                float.Parse(values[11], invariantCulture),
                                float.Parse(values[12], invariantCulture),
                                float.Parse(values[13], invariantCulture)
                            );
                            currentFrame.leftHandWorldRotation = new Quaternion(
                                float.Parse(values[14], invariantCulture),
                                float.Parse(values[15], invariantCulture),
                                float.Parse(values[16], invariantCulture),
                                float.Parse(values[17], invariantCulture)
                            );
                        }
                    }
                }
                else if (handType == "Right")
                {
                    currentFrame.rightLocalPoses[jointId] = poseData;

                    if (jointId == 0 && values.Length >= 18)
                    {
                        if (!string.IsNullOrEmpty(values[11]) &&
                            !string.IsNullOrEmpty(values[14]))
                        {
                            currentFrame.rightHandWorldPosition = new Vector3(
                                float.Parse(values[11], invariantCulture),
                                float.Parse(values[12], invariantCulture),
                                float.Parse(values[13], invariantCulture)
                            );
                            currentFrame.rightHandWorldRotation = new Quaternion(
                                float.Parse(values[14], invariantCulture),
                                float.Parse(values[15], invariantCulture),
                                float.Parse(values[16], invariantCulture),
                                float.Parse(values[17], invariantCulture)
                            );
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"라인 {i} 파싱 실패: {e.Message}\n라인 내용: {lines[i]}");
                continue;
            }
        }

        if (currentFrame != null) loadedSequence.Add(currentFrame);

        if (loadedSequence.Count == 0)
        {
            Debug.LogError("CSV 파싱 실패.");
            return;
        }

        if (playLeftHand)
        {
            isLeftPlaying = true;
            currentLeftPlaybackIndex = 0;
            lastLeftPlaybackTime = Time.time;
            ApplyLeftHandFrame();
        }

        if (playRightHand)
        {
            isRightPlaying = true;
            currentRightPlaybackIndex = 0;
            lastRightPlaybackTime = Time.time;
            ApplyRightHandFrame();
        }

        Debug.Log($"<color=cyan>재생 시작</color> - 총 {loadedSequence.Count} 프레임\n" +
                 $"기준점: {(referencePoint != null ? referencePoint.name : "없음")}\n" +
                 $"손 위치 비교: {(compareHandPosition ? "ON" : "OFF")}\n" +
                 $"손 회전 비교: {(compareHandRotation ? "ON" : "OFF")}");
    }

    public void StartLeftHandPlayback(string csvFileName)
    {
        playLeftHand = true;
        playRightHand = false;
        StartPlaybackFromCSV(csvFileName);
    }

    public void StartRightHandPlayback(string csvFileName)
    {
        playLeftHand = false;
        playRightHand = true;
        StartPlaybackFromCSV(csvFileName);
    }

    public void StartBothHandsPlayback(string csvFileName)
    {
        playLeftHand = true;
        playRightHand = true;
        StartPlaybackFromCSV(csvFileName);
    }

    public void StopPlayback()
    {
        isLeftPlaying = false;
        isRightPlaying = false;
        currentLeftPlaybackIndex = 0;
        currentRightPlaybackIndex = 0;
        Debug.Log("재생 중지.");
    }

    public void StopLeftHand()
    {
        isLeftPlaying = false;
        currentLeftPlaybackIndex = 0;
    }

    public void StopRightHand()
    {
        isRightPlaying = false;
        currentRightPlaybackIndex = 0;
    }

    private void ApplyLeftHandFrame()
    {
        if (currentLeftPlaybackIndex >= loadedSequence.Count)
            return;

        PoseFrame frame = loadedSequence[currentLeftPlaybackIndex];

        // 조인트 로컬 포즈 적용
        ApplyPosesToJoints(leftHandVisual, frame.leftLocalPoses);

        // 월드 위치/회전 적용 (기준점 기반)
        if (leftHandVisual != null)
        {
            Transform root = leftHandVisual.transform;
            if (root != null)
            {
                Vector3 targetWorldPos = frame.leftHandWorldPosition;
                Quaternion targetWorldRot = frame.leftHandWorldRotation;

                // 기준점이 있으면 기준점 기준으로 절대 위치 계산
                if (referencePoint != null)
                {
                    targetWorldPos = referencePoint.position + frame.leftHandWorldPosition;
                }

                root.position = targetWorldPos;
                root.rotation = targetWorldRot;

                // 디버그 로그 (처음 프레임만)
                if (currentLeftPlaybackIndex == 0 || currentLeftPlaybackIndex % 10 == 0)
                {
                    Debug.Log($"<color=cyan>왼손 프레임 {currentLeftPlaybackIndex}</color>\n" +
                             $"목표 위치: {targetWorldPos}\n" +
                             $"기준점: {(referencePoint != null ? referencePoint.position.ToString() : "없음")}\n" +
                             $"상대 위치: {frame.leftHandWorldPosition}");
                }
            }
        }
    }

    private void ApplyRightHandFrame()
    {
        if (currentRightPlaybackIndex >= loadedSequence.Count)
            return;

        PoseFrame frame = loadedSequence[currentRightPlaybackIndex];

        // 조인트 로컬 포즈 적용
        ApplyPosesToJoints(rightHandVisual, frame.rightLocalPoses);

        // 월드 위치/회전 적용 (기준점 기반)
        if (rightHandVisual != null)
        {
            Transform root = rightHandVisual.transform;
            if (root != null)
            {
                Vector3 targetWorldPos = frame.rightHandWorldPosition;
                Quaternion targetWorldRot = frame.rightHandWorldRotation;

                // 기준점이 있으면 기준점 기준으로 절대 위치 계산
                if (referencePoint != null)
                {
                    targetWorldPos = referencePoint.position + frame.rightHandWorldPosition;
                }

                root.position = targetWorldPos;
                root.rotation = targetWorldRot;

                // 디버그 로그
                if (currentRightPlaybackIndex == 0 || currentRightPlaybackIndex % 10 == 0)
                {
                    Debug.Log($"<color=cyan>오른손 프레임 {currentRightPlaybackIndex}</color>\n" +
                             $"목표 위치: {targetWorldPos}\n" +
                             $"기준점: {(referencePoint != null ? referencePoint.position.ToString() : "없음")}\n" +
                             $"상대 위치: {frame.rightHandWorldPosition}");
                }
            }
        }
    }

    private void ApplyPosesToJoints(HandVisual handVisual, Dictionary<int, PoseData> poses)
    {
        if (handVisual == null) return;

        for (int i = 0; i < handVisual.Joints.Count; i++)
        {
            if (poses.TryGetValue(i, out PoseData poseData) && handVisual.Joints[i] != null)
            {
                handVisual.Joints[i].localPosition = poseData.position;
                handVisual.Joints[i].localRotation = poseData.rotation;
            }
        }
    }

    public void ToggleAutoProgress(bool enable)
    {
        enableAutoProgress = enable;
    }

    public void SetHandPositionComparison(bool enable, float posThreshold, float rotThreshold)
    {
        compareHandPosition = enable;
        handPositionThreshold = posThreshold;
        handRotationThreshold = rotThreshold;
        Debug.Log($"손 위치 비교: {enable}, 위치 임계값: {posThreshold * 100}cm, 회전 임계값: {rotThreshold}°");
    }

    public void SetReferencePoint(Transform reference)
    {
        referencePoint = reference;
        Debug.Log($"기준점 설정: {(reference != null ? reference.name + " at " + reference.position : "없음")}");
    }

    public void SetReplayHandsVisible(bool visible)
    {
        showReplayHands = visible;

        if (leftHandVisual != null)
        {
            SkinnedMeshRenderer[] leftRenderers = leftHandVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in leftRenderers)
            {
                renderer.enabled = visible;
            }
        }

        if (rightHandVisual != null)
        {
            SkinnedMeshRenderer[] rightRenderers = rightHandVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in rightRenderers)
            {
                renderer.enabled = visible;
            }
        }

        Debug.Log($"리플레이 손 표시: {(visible ? "ON" : "OFF")}");
    }

    public void SetReplayHandAlpha(float alpha)
    {
        replayHandAlpha = Mathf.Clamp01(alpha);

        if (leftHandVisual != null)
        {
            SkinnedMeshRenderer[] leftRenderers = leftHandVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in leftRenderers)
            {
                Material mat = renderer.material;
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = replayHandAlpha;
                    mat.color = color;
                }
            }
        }

        if (rightHandVisual != null)
        {
            SkinnedMeshRenderer[] rightRenderers = rightHandVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in rightRenderers)
            {
                Material mat = renderer.material;
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = replayHandAlpha;
                    mat.color = color;
                }
            }
        }

        Debug.Log($"리플레이 손 투명도: {replayHandAlpha * 100}%");
    }

    public void SetDebugGizmos(bool show)
    {
        showDebugGizmos = show;
    }

    public void SetPositionLines(bool show)
    {
        showPositionLines = show;
    }

    public float GetLeftHandProgress()
    {
        if (loadedSequence.Count == 0) return 0f;
        return (float)currentLeftPlaybackIndex / loadedSequence.Count;
    }

    public float GetRightHandProgress()
    {
        if (loadedSequence.Count == 0) return 0f;
        return (float)currentRightPlaybackIndex / loadedSequence.Count;
    }

    public string GetStatusText()
    {
        string leftStatus = isLeftPlaying ? $"재생중 ({currentLeftPlaybackIndex + 1}/{loadedSequence.Count})" : "중지";
        string rightStatus = isRightPlaying ? $"재생중 ({currentRightPlaybackIndex + 1}/{loadedSequence.Count})" : "중지";

        return $"왼손: {leftStatus}\n" +
               $"  조인트 유사도: {currentLeftSimilarity * 100:F1}%\n" +
               $"  위치 오차: {leftHandPositionError * 100:F1}cm\n" +
               $"  회전 오차: {leftHandRotationError:F1}°\n" +
               $"오른손: {rightStatus}\n" +
               $"  조인트 유사도: {currentRightSimilarity * 100:F1}%\n" +
               $"  위치 오차: {rightHandPositionError * 100:F1}cm\n" +
               $"  회전 오차: {rightHandRotationError:F1}°";
    }

    public Vector3 GetLeftHandPosition()
    {
        if (playerLeftHand != null && playerLeftHand.Joints.Count > 0)
        {
            return playerLeftHand.Joints[(int)HandJointId.HandWristRoot].position;
        }
        return Vector3.zero;
    }

    public Vector3 GetRightHandPosition()
    {
        if (playerRightHand != null && playerRightHand.Joints.Count > 0)
        {
            return playerRightHand.Joints[(int)HandJointId.HandWristRoot].position;
        }
        return Vector3.zero;
    }

    public bool IsLeftHandPlaying() => isLeftPlaying;
    public bool IsRightHandPlaying() => isRightPlaying;

    // ========================================
    // 시나리오 연동 메서드
    // ========================================

    /// <summary>
    /// 시퀀스 완료 체크 (시나리오 연동용)
    /// </summary>
    private void CheckSequenceCompletion()
    {
        bool leftCompleted = !playLeftHand || !isLeftPlaying;
        bool rightCompleted = !playRightHand || !isRightPlaying;

        if (leftCompleted && rightCompleted && loadedSequence.Count > 0)
        {
            hasNotifiedCompletion = true;
            Debug.Log("<color=yellow>[HandPosePlayer] 시퀀스 완료 - 시나리오에 알림</color>");
            OnSequenceCompleted?.Invoke();
        }
    }

    /// <summary>
    /// 시퀀스 재시작 시 플래그 초기화
    /// </summary>
    private void ResetCompletionFlag()
    {
        hasNotifiedCompletion = false;
    }
}