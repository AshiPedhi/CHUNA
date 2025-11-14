using UnityEngine;
using System.Collections;

/// <summary>
/// 유사도에 따라 환자 모델의 움직임을 제어하는 컨트롤러
/// HandPosePlayer의 유사도를 받아서 환자 모델을 진행시킴
/// </summary>
public class PatientModelController : MonoBehaviour
{
    [Header("=== 환자 모델 설정 ===")]
    [SerializeField] private GameObject patientModel;
    [SerializeField] private Animator patientAnimator;

    [Header("=== 상체 조인트 참조 (자동 매핑 가능) ===")]
    [SerializeField] private Transform patientSpine;
    [SerializeField] private Transform patientNeck;
    [SerializeField] private Transform patientHead;
    [SerializeField] private Transform patientChest;
    [SerializeField] private Transform patientUpperChest;

    [Header("=== 왼쪽 팔 조인트 ===")]
    [SerializeField] private Transform patientLeftShoulder;
    [SerializeField] private Transform patientLeftUpperArm;
    [SerializeField] private Transform patientLeftLowerArm;
    [SerializeField] private Transform patientLeftHand;

    [Header("=== 오른쪽 팔 조인트 ===")]
    [SerializeField] private Transform patientRightShoulder;
    [SerializeField] private Transform patientRightUpperArm;
    [SerializeField] private Transform patientRightLowerArm;
    [SerializeField] private Transform patientRightHand;

    [Header("=== 추가 조인트 ===")]
    [SerializeField] private Transform[] patientJoints;

    [Header("=== 자동 매핑 설정 ===")]
    [SerializeField] private bool autoMapOnStart = true;
    [SerializeField] private bool useAnimator = true;
    [SerializeField] private bool useFallbackNaming = true;

    [Header("=== HandPosePlayer 연결 ===")]
    [SerializeField] private HandPosePlayer handPosePlayer;

    [Header("=== 진행 설정 ===")]
    [SerializeField] private float similarityThreshold = 0.7f;  // 진행 임계값
    [SerializeField] private float requiredHoldTime = 2.0f;     // 임계값 유지 시간
    [SerializeField] private float progressSpeed = 0.5f;        // 진행 속도
    [SerializeField] private bool requireBothHands = true;      // 양손 모두 필요

    [Header("=== 움직임 설정 ===")]
    [SerializeField] private bool useAnimationCurve = true;
    [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("상체 최대 회전 각도")]
    [SerializeField] private float maxRotation = 30f;

    [Tooltip("머리 최대 이동 거리")]
    [SerializeField] private float maxDisplacement = 0.1f;

    [Header("=== 팔 움직임 설정 ===")]
    [Tooltip("팔을 들어올리는 최대 각도")]
    [SerializeField] private float maxArmRaise = 60f;

    [Tooltip("팔꿈치를 굽히는 최대 각도")]
    [SerializeField] private float maxElbowBend = 90f;

    [Tooltip("어깨 회전 활성화")]
    [SerializeField] private bool enableShoulderRotation = true;

    [Tooltip("팔꿈치 굽힘 활성화")]
    [SerializeField] private bool enableElbowBend = true;

    [Tooltip("양팔 동시 움직임")]
    [SerializeField] private bool moveBothArms = true;

    [Header("=== 시각적 피드백 ===")]
    [SerializeField] private bool showProgressIndicator = true;
    [SerializeField] private GameObject progressIndicatorPrefab;
    [SerializeField] private Color lowProgressColor = Color.red;
    [SerializeField] private Color midProgressColor = Color.yellow;
    [SerializeField] private Color highProgressColor = Color.green;

    // 상태 변수
    private float currentProgress = 0f;           // 0 ~ 1
    private float similarityHoldTime = 0f;
    private bool isProgressing = false;
    private bool isCompleted = false;

    // 초기 포즈 저장
    private Vector3 initialSpinePosition;
    private Quaternion initialSpineRotation;
    private Vector3 initialChestPosition;
    private Quaternion initialChestRotation;
    private Vector3 initialUpperChestPosition;
    private Quaternion initialUpperChestRotation;
    private Vector3 initialNeckPosition;
    private Quaternion initialNeckRotation;
    private Vector3 initialHeadPosition;
    private Quaternion initialHeadRotation;

    // 왼쪽 팔 초기 포즈
    private Vector3 initialLeftShoulderPosition;
    private Quaternion initialLeftShoulderRotation;
    private Vector3 initialLeftUpperArmPosition;
    private Quaternion initialLeftUpperArmRotation;
    private Vector3 initialLeftLowerArmPosition;
    private Quaternion initialLeftLowerArmRotation;
    private Vector3 initialLeftHandPosition;
    private Quaternion initialLeftHandRotation;

    // 오른쪽 팔 초기 포즈
    private Vector3 initialRightShoulderPosition;
    private Quaternion initialRightShoulderRotation;
    private Vector3 initialRightUpperArmPosition;
    private Quaternion initialRightUpperArmRotation;
    private Vector3 initialRightLowerArmPosition;
    private Quaternion initialRightLowerArmRotation;
    private Vector3 initialRightHandPosition;
    private Quaternion initialRightHandRotation;

    // 진행 표시기
    private GameObject activeProgressIndicator;
    private Renderer progressRenderer;

    // 이벤트
    public System.Action<float> OnProgressUpdated;
    public System.Action OnMovementCompleted;
    public System.Action OnThresholdMet;

    // 공개 프로퍼티
    public float CurrentProgress => currentProgress;
    public bool IsCompleted => isCompleted;
    public bool IsProgressing => isProgressing;

    private void Awake()
    {
        InitializeComponents();
        SaveInitialPose();
    }

    private void Start()
    {
        if (showProgressIndicator && progressIndicatorPrefab != null)
        {
            CreateProgressIndicator();
        }
    }

    private void InitializeComponents()
    {
        if (patientModel == null)
        {
            Debug.LogWarning("[PatientModelController] 환자 모델이 설정되지 않았습니다.");
            return;
        }

        if (handPosePlayer == null)
        {
            handPosePlayer = FindObjectOfType<HandPosePlayer>();
            if (handPosePlayer != null)
            {
                Debug.Log("[PatientModelController] HandPosePlayer 자동으로 찾음");
            }
        }

        // Animator 컴포넌트 찾기
        if (patientAnimator == null)
        {
            patientAnimator = patientModel.GetComponent<Animator>();
            if (patientAnimator == null)
            {
                patientAnimator = patientModel.GetComponentInChildren<Animator>();
            }
        }

        // 자동 매핑 실행
        if (autoMapOnStart)
        {
            AutoMapJoints();
        }
    }

    /// <summary>
    /// 조인트 자동 매핑
    /// </summary>
    private void AutoMapJoints()
    {
        if (patientModel == null)
        {
            Debug.LogError("[PatientModelController] 환자 모델이 없어 자동 매핑을 수행할 수 없습니다.");
            return;
        }

        Debug.Log("[PatientModelController] 조인트 자동 매핑 시작...");

        bool success = false;

        // 방법 1: Animator의 HumanBodyBones 사용
        if (useAnimator && patientAnimator != null && patientAnimator.isHuman)
        {
            success = MapJointsFromAnimator();
        }

        // 방법 2: 이름 기반 검색 (Animator 실패 시 또는 fallback)
        if (!success && useFallbackNaming)
        {
            success = MapJointsByName();
        }

        // 매핑 결과 검증
        ValidateMapping();
    }

    /// <summary>
    /// Animator를 사용한 자동 매핑 (Humanoid 리그)
    /// </summary>
    private bool MapJointsFromAnimator()
    {
        if (patientAnimator == null || !patientAnimator.isHuman)
        {
            Debug.LogWarning("[PatientModelController] Animator가 Humanoid 타입이 아닙니다.");
            return false;
        }

        Debug.Log("[PatientModelController] Animator (Humanoid) 기반 자동 매핑 시도...");

        int mappedCount = 0;

        // Spine 매핑
        if (patientSpine == null)
        {
            patientSpine = patientAnimator.GetBoneTransform(HumanBodyBones.Spine);
            if (patientSpine != null)
            {
                Debug.Log($"[PatientModelController] ✓ Spine 매핑: {patientSpine.name}");
                mappedCount++;
            }
        }

        // Chest 매핑
        if (patientChest == null)
        {
            patientChest = patientAnimator.GetBoneTransform(HumanBodyBones.Chest);
            if (patientChest != null)
            {
                Debug.Log($"[PatientModelController] ✓ Chest 매핑: {patientChest.name}");
                mappedCount++;
            }
        }

        // Upper Chest 매핑
        if (patientUpperChest == null)
        {
            patientUpperChest = patientAnimator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (patientUpperChest != null)
            {
                Debug.Log($"[PatientModelController] ✓ Upper Chest 매핑: {patientUpperChest.name}");
                mappedCount++;
            }
        }

        // Neck 매핑
        if (patientNeck == null)
        {
            patientNeck = patientAnimator.GetBoneTransform(HumanBodyBones.Neck);
            if (patientNeck != null)
            {
                Debug.Log($"[PatientModelController] ✓ Neck 매핑: {patientNeck.name}");
                mappedCount++;
            }
        }

        // Head 매핑
        if (patientHead == null)
        {
            patientHead = patientAnimator.GetBoneTransform(HumanBodyBones.Head);
            if (patientHead != null)
            {
                Debug.Log($"[PatientModelController] ✓ Head 매핑: {patientHead.name}");
                mappedCount++;
            }
        }

        // === 왼쪽 팔 매핑 ===
        if (patientLeftShoulder == null)
        {
            patientLeftShoulder = patientAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            if (patientLeftShoulder != null)
            {
                Debug.Log($"[PatientModelController] ✓ Left Shoulder 매핑: {patientLeftShoulder.name}");
                mappedCount++;
            }
        }

        if (patientLeftUpperArm == null)
        {
            patientLeftUpperArm = patientAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (patientLeftUpperArm != null)
            {
                Debug.Log($"[PatientModelController] ✓ Left Upper Arm 매핑: {patientLeftUpperArm.name}");
                mappedCount++;
            }
        }

        if (patientLeftLowerArm == null)
        {
            patientLeftLowerArm = patientAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            if (patientLeftLowerArm != null)
            {
                Debug.Log($"[PatientModelController] ✓ Left Lower Arm 매핑: {patientLeftLowerArm.name}");
                mappedCount++;
            }
        }

        if (patientLeftHand == null)
        {
            patientLeftHand = patientAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (patientLeftHand != null)
            {
                Debug.Log($"[PatientModelController] ✓ Left Hand 매핑: {patientLeftHand.name}");
                mappedCount++;
            }
        }

        // === 오른쪽 팔 매핑 ===
        if (patientRightShoulder == null)
        {
            patientRightShoulder = patientAnimator.GetBoneTransform(HumanBodyBones.RightShoulder);
            if (patientRightShoulder != null)
            {
                Debug.Log($"[PatientModelController] ✓ Right Shoulder 매핑: {patientRightShoulder.name}");
                mappedCount++;
            }
        }

        if (patientRightUpperArm == null)
        {
            patientRightUpperArm = patientAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (patientRightUpperArm != null)
            {
                Debug.Log($"[PatientModelController] ✓ Right Upper Arm 매핑: {patientRightUpperArm.name}");
                mappedCount++;
            }
        }

        if (patientRightLowerArm == null)
        {
            patientRightLowerArm = patientAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (patientRightLowerArm != null)
            {
                Debug.Log($"[PatientModelController] ✓ Right Lower Arm 매핑: {patientRightLowerArm.name}");
                mappedCount++;
            }
        }

        if (patientRightHand == null)
        {
            patientRightHand = patientAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            if (patientRightHand != null)
            {
                Debug.Log($"[PatientModelController] ✓ Right Hand 매핑: {patientRightHand.name}");
                mappedCount++;
            }
        }

        Debug.Log($"[PatientModelController] Animator 기반 매핑 완료: {mappedCount}개 조인트");

        return mappedCount > 0;
    }

    /// <summary>
    /// 이름 기반 자동 매핑 (Fallback)
    /// </summary>
    private bool MapJointsByName()
    {
        Debug.Log("[PatientModelController] 이름 기반 자동 매핑 시도...");

        int mappedCount = 0;
        Transform[] allTransforms = patientModel.GetComponentsInChildren<Transform>();

        foreach (Transform t in allTransforms)
        {
            string lowerName = t.name.ToLower();

            // Spine 찾기
            if (patientSpine == null && (
                lowerName.Contains("spine") ||
                lowerName.Contains("hips") ||
                lowerName == "pelvis"))
            {
                patientSpine = t;
                Debug.Log($"[PatientModelController] ✓ Spine 매핑 (이름): {t.name}");
                mappedCount++;
            }

            // Chest 찾기
            if (patientChest == null && (
                lowerName.Contains("chest") ||
                lowerName.Contains("thorax")))
            {
                patientChest = t;
                Debug.Log($"[PatientModelController] ✓ Chest 매핑 (이름): {t.name}");
                mappedCount++;
            }

            // Upper Chest 찾기
            if (patientUpperChest == null && (
                lowerName.Contains("upperchest") ||
                lowerName.Contains("upper chest") ||
                lowerName.Contains("upperthorax")))
            {
                patientUpperChest = t;
                Debug.Log($"[PatientModelController] ✓ Upper Chest 매핑 (이름): {t.name}");
                mappedCount++;
            }

            // Neck 찾기
            if (patientNeck == null && lowerName.Contains("neck"))
            {
                patientNeck = t;
                Debug.Log($"[PatientModelController] ✓ Neck 매핑 (이름): {t.name}");
                mappedCount++;
            }

            // Head 찾기
            if (patientHead == null && lowerName == "head")
            {
                patientHead = t;
                Debug.Log($"[PatientModelController] ✓ Head 매핑 (이름): {t.name}");
                mappedCount++;
            }

            // === 왼쪽 팔 찾기 ===
            if (patientLeftShoulder == null && (
                (lowerName.Contains("left") && lowerName.Contains("shoulder")) ||
                (lowerName.Contains("left") && lowerName.Contains("clavicle")) ||
                lowerName.Contains("l_shoulder") ||
                lowerName.Contains("leftshoulder")))
            {
                patientLeftShoulder = t;
                Debug.Log($"[PatientModelController] ✓ Left Shoulder 매핑 (이름): {t.name}");
                mappedCount++;
            }

            if (patientLeftUpperArm == null && (
                (lowerName.Contains("left") && (lowerName.Contains("upperarm") || lowerName.Contains("upper arm"))) ||
                lowerName.Contains("l_upperarm") ||
                lowerName.Contains("leftupperarm") ||
                (lowerName.Contains("left") && lowerName.Contains("arm") && !lowerName.Contains("lower") && !lowerName.Contains("fore"))))
            {
                patientLeftUpperArm = t;
                Debug.Log($"[PatientModelController] ✓ Left Upper Arm 매핑 (이름): {t.name}");
                mappedCount++;
            }

            if (patientLeftLowerArm == null && (
                (lowerName.Contains("left") && (lowerName.Contains("lowerarm") || lowerName.Contains("lower arm") || lowerName.Contains("forearm"))) ||
                lowerName.Contains("l_lowerarm") ||
                lowerName.Contains("leftlowerarm") ||
                lowerName.Contains("l_forearm")))
            {
                patientLeftLowerArm = t;
                Debug.Log($"[PatientModelController] ✓ Left Lower Arm 매핑 (이름): {t.name}");
                mappedCount++;
            }

            if (patientLeftHand == null && (
                (lowerName.Contains("left") && lowerName.Contains("hand")) ||
                lowerName.Contains("l_hand") ||
                lowerName == "lefthand"))
            {
                patientLeftHand = t;
                Debug.Log($"[PatientModelController] ✓ Left Hand 매핑 (이름): {t.name}");
                mappedCount++;
            }

            // === 오른쪽 팔 찾기 ===
            if (patientRightShoulder == null && (
                (lowerName.Contains("right") && lowerName.Contains("shoulder")) ||
                (lowerName.Contains("right") && lowerName.Contains("clavicle")) ||
                lowerName.Contains("r_shoulder") ||
                lowerName.Contains("rightshoulder")))
            {
                patientRightShoulder = t;
                Debug.Log($"[PatientModelController] ✓ Right Shoulder 매핑 (이름): {t.name}");
                mappedCount++;
            }

            if (patientRightUpperArm == null && (
                (lowerName.Contains("right") && (lowerName.Contains("upperarm") || lowerName.Contains("upper arm"))) ||
                lowerName.Contains("r_upperarm") ||
                lowerName.Contains("rightupperarm") ||
                (lowerName.Contains("right") && lowerName.Contains("arm") && !lowerName.Contains("lower") && !lowerName.Contains("fore"))))
            {
                patientRightUpperArm = t;
                Debug.Log($"[PatientModelController] ✓ Right Upper Arm 매핑 (이름): {t.name}");
                mappedCount++;
            }

            if (patientRightLowerArm == null && (
                (lowerName.Contains("right") && (lowerName.Contains("lowerarm") || lowerName.Contains("lower arm") || lowerName.Contains("forearm"))) ||
                lowerName.Contains("r_lowerarm") ||
                lowerName.Contains("rightlowerarm") ||
                lowerName.Contains("r_forearm")))
            {
                patientRightLowerArm = t;
                Debug.Log($"[PatientModelController] ✓ Right Lower Arm 매핑 (이름): {t.name}");
                mappedCount++;
            }

            if (patientRightHand == null && (
                (lowerName.Contains("right") && lowerName.Contains("hand")) ||
                lowerName.Contains("r_hand") ||
                lowerName == "righthand"))
            {
                patientRightHand = t;
                Debug.Log($"[PatientModelController] ✓ Right Hand 매핑 (이름): {t.name}");
                mappedCount++;
            }
        }

        Debug.Log($"[PatientModelController] 이름 기반 매핑 완료: {mappedCount}개 조인트");

        return mappedCount > 0;
    }

    /// <summary>
    /// 매핑 결과 검증
    /// </summary>
    private void ValidateMapping()
    {
        Debug.Log("=== [PatientModelController] 조인트 매핑 결과 ===");

        int totalMapped = 0;

        // 상체
        if (patientSpine != null)
        {
            Debug.Log($"  ✓ Spine: {patientSpine.name}");
            totalMapped++;
        }
        else
        {
            Debug.LogWarning("  ✗ Spine: 매핑 실패");
        }

        if (patientChest != null)
        {
            Debug.Log($"  ✓ Chest: {patientChest.name}");
            totalMapped++;
        }

        if (patientUpperChest != null)
        {
            Debug.Log($"  ✓ Upper Chest: {patientUpperChest.name}");
            totalMapped++;
        }

        if (patientNeck != null)
        {
            Debug.Log($"  ✓ Neck: {patientNeck.name}");
            totalMapped++;
        }
        else
        {
            Debug.LogWarning("  ✗ Neck: 매핑 실패");
        }

        if (patientHead != null)
        {
            Debug.Log($"  ✓ Head: {patientHead.name}");
            totalMapped++;
        }
        else
        {
            Debug.LogWarning("  ✗ Head: 매핑 실패");
        }

        // 왼쪽 팔
        Debug.Log("  --- 왼쪽 팔 ---");
        if (patientLeftShoulder != null)
        {
            Debug.Log($"  ✓ Left Shoulder: {patientLeftShoulder.name}");
            totalMapped++;
        }

        if (patientLeftUpperArm != null)
        {
            Debug.Log($"  ✓ Left Upper Arm: {patientLeftUpperArm.name}");
            totalMapped++;
        }

        if (patientLeftLowerArm != null)
        {
            Debug.Log($"  ✓ Left Lower Arm: {patientLeftLowerArm.name}");
            totalMapped++;
        }

        if (patientLeftHand != null)
        {
            Debug.Log($"  ✓ Left Hand: {patientLeftHand.name}");
            totalMapped++;
        }

        // 오른쪽 팔
        Debug.Log("  --- 오른쪽 팔 ---");
        if (patientRightShoulder != null)
        {
            Debug.Log($"  ✓ Right Shoulder: {patientRightShoulder.name}");
            totalMapped++;
        }

        if (patientRightUpperArm != null)
        {
            Debug.Log($"  ✓ Right Upper Arm: {patientRightUpperArm.name}");
            totalMapped++;
        }

        if (patientRightLowerArm != null)
        {
            Debug.Log($"  ✓ Right Lower Arm: {patientRightLowerArm.name}");
            totalMapped++;
        }

        if (patientRightHand != null)
        {
            Debug.Log($"  ✓ Right Hand: {patientRightHand.name}");
            totalMapped++;
        }

        Debug.Log($"=== 총 {totalMapped}개 조인트 매핑 완료 ===");

        if (totalMapped == 0)
        {
            Debug.LogError("[PatientModelController] 조인트 매핑에 실패했습니다! 수동으로 설정하거나 환자 모델 구조를 확인하세요.");
        }
        else if (totalMapped < 5)
        {
            Debug.LogWarning("[PatientModelController] 필수 조인트 일부만 매핑되었습니다. 필요한 경우 수동으로 추가 설정하세요.");
        }
        else
        {
            Debug.Log($"[PatientModelController] ✅ 조인트 매핑 성공! ({totalMapped}개)");
        }
    }

    private void SaveInitialPose()
    {
        if (patientSpine != null)
        {
            initialSpinePosition = patientSpine.localPosition;
            initialSpineRotation = patientSpine.localRotation;
        }

        if (patientChest != null)
        {
            initialChestPosition = patientChest.localPosition;
            initialChestRotation = patientChest.localRotation;
        }

        if (patientUpperChest != null)
        {
            initialUpperChestPosition = patientUpperChest.localPosition;
            initialUpperChestRotation = patientUpperChest.localRotation;
        }

        if (patientNeck != null)
        {
            initialNeckPosition = patientNeck.localPosition;
            initialNeckRotation = patientNeck.localRotation;
        }

        if (patientHead != null)
        {
            initialHeadPosition = patientHead.localPosition;
            initialHeadRotation = patientHead.localRotation;
        }

        // 왼쪽 팔 초기 포즈 저장
        if (patientLeftShoulder != null)
        {
            initialLeftShoulderPosition = patientLeftShoulder.localPosition;
            initialLeftShoulderRotation = patientLeftShoulder.localRotation;
        }

        if (patientLeftUpperArm != null)
        {
            initialLeftUpperArmPosition = patientLeftUpperArm.localPosition;
            initialLeftUpperArmRotation = patientLeftUpperArm.localRotation;
        }

        if (patientLeftLowerArm != null)
        {
            initialLeftLowerArmPosition = patientLeftLowerArm.localPosition;
            initialLeftLowerArmRotation = patientLeftLowerArm.localRotation;
        }

        if (patientLeftHand != null)
        {
            initialLeftHandPosition = patientLeftHand.localPosition;
            initialLeftHandRotation = patientLeftHand.localRotation;
        }

        // 오른쪽 팔 초기 포즈 저장
        if (patientRightShoulder != null)
        {
            initialRightShoulderPosition = patientRightShoulder.localPosition;
            initialRightShoulderRotation = patientRightShoulder.localRotation;
        }

        if (patientRightUpperArm != null)
        {
            initialRightUpperArmPosition = patientRightUpperArm.localPosition;
            initialRightUpperArmRotation = patientRightUpperArm.localRotation;
        }

        if (patientRightLowerArm != null)
        {
            initialRightLowerArmPosition = patientRightLowerArm.localPosition;
            initialRightLowerArmRotation = patientRightLowerArm.localRotation;
        }

        if (patientRightHand != null)
        {
            initialRightHandPosition = patientRightHand.localPosition;
            initialRightHandRotation = patientRightHand.localRotation;
        }
    }

    private void Update()
    {
        if (!isProgressing || isCompleted) return;

        UpdateSimilarityCheck();
        UpdatePatientPose();
        UpdateVisualFeedback();
    }

    /// <summary>
    /// 유사도 체크 및 진행도 업데이트
    /// </summary>
    private void UpdateSimilarityCheck()
    {
        if (handPosePlayer == null) return;

        float leftSimilarity = handPosePlayer.GetLeftSimilarity();
        float rightSimilarity = handPosePlayer.GetRightSimilarity();

        bool meetsThreshold = false;

        if (requireBothHands)
        {
            meetsThreshold = (leftSimilarity >= similarityThreshold &&
                            rightSimilarity >= similarityThreshold);
        }
        else
        {
            float avgSimilarity = (leftSimilarity + rightSimilarity) / 2f;
            meetsThreshold = avgSimilarity >= similarityThreshold;
        }

        if (meetsThreshold)
        {
            similarityHoldTime += Time.deltaTime;

            if (similarityHoldTime >= requiredHoldTime && currentProgress < 1f)
            {
                // 진행도 증가
                currentProgress += progressSpeed * Time.deltaTime;
                currentProgress = Mathf.Clamp01(currentProgress);

                OnProgressUpdated?.Invoke(currentProgress);

                if (currentProgress >= 1f)
                {
                    CompleteMovement();
                }
            }
            else if (similarityHoldTime >= requiredHoldTime)
            {
                OnThresholdMet?.Invoke();
            }
        }
        else
        {
            // 임계값 미달 시 holdTime 감소
            similarityHoldTime -= Time.deltaTime * 0.5f;
            similarityHoldTime = Mathf.Max(0f, similarityHoldTime);
        }
    }

    /// <summary>
    /// 환자 포즈 업데이트
    /// </summary>
    private void UpdatePatientPose()
    {
        float curvedProgress = useAnimationCurve ?
            progressCurve.Evaluate(currentProgress) : currentProgress;

        // Spine 회전
        if (patientSpine != null)
        {
            Quaternion targetRotation = initialSpineRotation *
                Quaternion.Euler(maxRotation * curvedProgress, 0, 0);
            patientSpine.localRotation = Quaternion.Lerp(
                patientSpine.localRotation, targetRotation, Time.deltaTime * 5f);
        }

        // Chest 회전 (Spine보다 적게)
        if (patientChest != null)
        {
            Quaternion targetRotation = Quaternion.Euler(maxRotation * curvedProgress * 0.7f, 0, 0);
            patientChest.localRotation = Quaternion.Lerp(
                patientChest.localRotation, targetRotation, Time.deltaTime * 5f);
        }

        // Upper Chest 회전 (더 적게)
        if (patientUpperChest != null)
        {
            Quaternion targetRotation = Quaternion.Euler(maxRotation * curvedProgress * 0.5f, 0, 0);
            patientUpperChest.localRotation = Quaternion.Lerp(
                patientUpperChest.localRotation, targetRotation, Time.deltaTime * 5f);
        }

        // Neck 회전
        if (patientNeck != null)
        {
            Quaternion targetRotation = initialNeckRotation *
                Quaternion.Euler(maxRotation * curvedProgress * 0.5f, 0, 0);
            patientNeck.localRotation = Quaternion.Lerp(
                patientNeck.localRotation, targetRotation, Time.deltaTime * 5f);
        }

        // Head 이동
        if (patientHead != null)
        {
            Vector3 targetPosition = initialHeadPosition +
                Vector3.up * maxDisplacement * curvedProgress;
            patientHead.localPosition = Vector3.Lerp(
                patientHead.localPosition, targetPosition, Time.deltaTime * 5f);
        }

        // === 왼쪽 팔 움직임 ===
        if (moveBothArms || true)  // 항상 왼팔 움직임
        {
            // 왼쪽 어깨 - 팔을 들어올림
            if (patientLeftShoulder != null && enableShoulderRotation)
            {
                Quaternion targetRotation = initialLeftShoulderRotation *
                    Quaternion.Euler(0, 0, maxArmRaise * curvedProgress * 0.3f);
                patientLeftShoulder.localRotation = Quaternion.Lerp(
                    patientLeftShoulder.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 왼쪽 상완 - 팔을 들어올림
            if (patientLeftUpperArm != null && enableShoulderRotation)
            {
                Quaternion targetRotation = initialLeftUpperArmRotation *
                    Quaternion.Euler(-maxArmRaise * curvedProgress, 0, 0);
                patientLeftUpperArm.localRotation = Quaternion.Lerp(
                    patientLeftUpperArm.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 왼쪽 전완 - 팔꿈치 굽힘
            if (patientLeftLowerArm != null && enableElbowBend)
            {
                Quaternion targetRotation = initialLeftLowerArmRotation *
                    Quaternion.Euler(0, 0, -maxElbowBend * curvedProgress);
                patientLeftLowerArm.localRotation = Quaternion.Lerp(
                    patientLeftLowerArm.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 왼쪽 손 - 약간의 회전
            if (patientLeftHand != null)
            {
                Quaternion targetRotation = initialLeftHandRotation *
                    Quaternion.Euler(0, 0, -15f * curvedProgress);
                patientLeftHand.localRotation = Quaternion.Lerp(
                    patientLeftHand.localRotation, targetRotation, Time.deltaTime * 5f);
            }
        }

        // === 오른쪽 팔 움직임 ===
        if (moveBothArms)
        {
            // 오른쪽 어깨 - 팔을 들어올림
            if (patientRightShoulder != null && enableShoulderRotation)
            {
                Quaternion targetRotation = initialRightShoulderRotation *
                    Quaternion.Euler(0, 0, -maxArmRaise * curvedProgress * 0.3f);
                patientRightShoulder.localRotation = Quaternion.Lerp(
                    patientRightShoulder.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 오른쪽 상완 - 팔을 들어올림
            if (patientRightUpperArm != null && enableShoulderRotation)
            {
                Quaternion targetRotation = initialRightUpperArmRotation *
                    Quaternion.Euler(-maxArmRaise * curvedProgress, 0, 0);
                patientRightUpperArm.localRotation = Quaternion.Lerp(
                    patientRightUpperArm.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 오른쪽 전완 - 팔꿈치 굽힘
            if (patientRightLowerArm != null && enableElbowBend)
            {
                Quaternion targetRotation = initialRightLowerArmRotation *
                    Quaternion.Euler(0, 0, maxElbowBend * curvedProgress);
                patientRightLowerArm.localRotation = Quaternion.Lerp(
                    patientRightLowerArm.localRotation, targetRotation, Time.deltaTime * 5f);
            }

            // 오른쪽 손 - 약간의 회전
            if (patientRightHand != null)
            {
                Quaternion targetRotation = initialRightHandRotation *
                    Quaternion.Euler(0, 0, 15f * curvedProgress);
                patientRightHand.localRotation = Quaternion.Lerp(
                    patientRightHand.localRotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }

    /// <summary>
    /// 시각적 피드백 업데이트
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (progressRenderer == null) return;

        Color targetColor;
        if (currentProgress < 0.33f)
            targetColor = lowProgressColor;
        else if (currentProgress < 0.67f)
            targetColor = midProgressColor;
        else
            targetColor = highProgressColor;

        progressRenderer.material.color = Color.Lerp(
            progressRenderer.material.color, targetColor, Time.deltaTime * 3f);
    }

    /// <summary>
    /// 진행 표시기 생성
    /// </summary>
    private void CreateProgressIndicator()
    {
        if (patientHead != null)
        {
            activeProgressIndicator = Instantiate(progressIndicatorPrefab,
                patientHead.position + Vector3.up * 0.3f, Quaternion.identity);
            activeProgressIndicator.transform.SetParent(patientHead);

            progressRenderer = activeProgressIndicator.GetComponent<Renderer>();
        }
    }

    /// <summary>
    /// 움직임 완료
    /// </summary>
    private void CompleteMovement()
    {
        isCompleted = true;
        isProgressing = false;

        Debug.Log("[PatientModelController] 환자 모델 움직임 완료!");

        OnMovementCompleted?.Invoke();
    }

    // ===== 공개 메서드 =====

    /// <summary>
    /// 진행 시작
    /// </summary>
    public void StartProgression()
    {
        isProgressing = true;
        isCompleted = false;
        currentProgress = 0f;
        similarityHoldTime = 0f;

        Debug.Log("[PatientModelController] 진행 시작");
    }

    /// <summary>
    /// 진행 중지
    /// </summary>
    public void StopProgression()
    {
        isProgressing = false;

        Debug.Log("[PatientModelController] 진행 중지");
    }

    /// <summary>
    /// 초기 포즈로 리셋
    /// </summary>
    public void ResetPose()
    {
        currentProgress = 0f;
        similarityHoldTime = 0f;
        isCompleted = false;

        if (patientSpine != null)
        {
            patientSpine.localPosition = initialSpinePosition;
            patientSpine.localRotation = initialSpineRotation;
        }

        if (patientChest != null)
        {
            patientChest.localPosition = initialChestPosition;
            patientChest.localRotation = initialChestRotation;
        }

        if (patientUpperChest != null)
        {
            patientUpperChest.localPosition = initialUpperChestPosition;
            patientUpperChest.localRotation = initialUpperChestRotation;
        }

        if (patientNeck != null)
        {
            patientNeck.localPosition = initialNeckPosition;
            patientNeck.localRotation = initialNeckRotation;
        }

        if (patientHead != null)
        {
            patientHead.localPosition = initialHeadPosition;
            patientHead.localRotation = initialHeadRotation;
        }

        // 왼쪽 팔 리셋
        if (patientLeftShoulder != null)
        {
            patientLeftShoulder.localPosition = initialLeftShoulderPosition;
            patientLeftShoulder.localRotation = initialLeftShoulderRotation;
        }

        if (patientLeftUpperArm != null)
        {
            patientLeftUpperArm.localPosition = initialLeftUpperArmPosition;
            patientLeftUpperArm.localRotation = initialLeftUpperArmRotation;
        }

        if (patientLeftLowerArm != null)
        {
            patientLeftLowerArm.localPosition = initialLeftLowerArmPosition;
            patientLeftLowerArm.localRotation = initialLeftLowerArmRotation;
        }

        if (patientLeftHand != null)
        {
            patientLeftHand.localPosition = initialLeftHandPosition;
            patientLeftHand.localRotation = initialLeftHandRotation;
        }

        // 오른쪽 팔 리셋
        if (patientRightShoulder != null)
        {
            patientRightShoulder.localPosition = initialRightShoulderPosition;
            patientRightShoulder.localRotation = initialRightShoulderRotation;
        }

        if (patientRightUpperArm != null)
        {
            patientRightUpperArm.localPosition = initialRightUpperArmPosition;
            patientRightUpperArm.localRotation = initialRightUpperArmRotation;
        }

        if (patientRightLowerArm != null)
        {
            patientRightLowerArm.localPosition = initialRightLowerArmPosition;
            patientRightLowerArm.localRotation = initialRightLowerArmRotation;
        }

        if (patientRightHand != null)
        {
            patientRightHand.localPosition = initialRightHandPosition;
            patientRightHand.localRotation = initialRightHandRotation;
        }

        Debug.Log("[PatientModelController] 포즈 리셋");
    }

    /// <summary>
    /// 설정 변경
    /// </summary>
    public void SetThreshold(float threshold)
    {
        similarityThreshold = Mathf.Clamp01(threshold);
    }

    public void SetHoldTime(float time)
    {
        requiredHoldTime = Mathf.Max(0f, time);
    }

    public void SetProgressSpeed(float speed)
    {
        progressSpeed = Mathf.Max(0f, speed);
    }

    public void SetRequireBothHands(bool require)
    {
        requireBothHands = require;
    }

    /// <summary>
    /// 팔 움직임 설정
    /// </summary>
    public void SetArmMovement(bool enableArms, bool bothArms = true)
    {
        enableShoulderRotation = enableArms;
        enableElbowBend = enableArms;
        moveBothArms = bothArms;
    }

    public void SetMaxArmRaise(float angle)
    {
        maxArmRaise = Mathf.Clamp(angle, 0f, 180f);
    }

    public void SetMaxElbowBend(float angle)
    {
        maxElbowBend = Mathf.Clamp(angle, 0f, 180f);
    }

    /// <summary>
    /// 수동 진행도 설정
    /// </summary>
    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        UpdatePatientPose();
    }

    /// <summary>
    /// 현재 유사도 정보 가져오기
    /// </summary>
    public (float left, float right, float hold) GetCurrentSimilarity()
    {
        if (handPosePlayer == null) return (0f, 0f, 0f);

        return (
            handPosePlayer.GetLeftSimilarity(),
            handPosePlayer.GetRightSimilarity(),
            similarityHoldTime
        );
    }

    // ===== 디버그 메서드 =====

    [ContextMenu("🔧 조인트 자동 매핑 실행")]
    private void ManualAutoMap()
    {
        AutoMapJoints();
    }

    [ContextMenu("📋 매핑 상태 확인")]
    private void CheckMappingStatus()
    {
        ValidateMapping();
    }

    [ContextMenu("테스트 - 진행 시작")]
    private void TestStartProgression()
    {
        StartProgression();
    }

    [ContextMenu("테스트 - 진행 중지")]
    private void TestStopProgression()
    {
        StopProgression();
    }

    [ContextMenu("테스트 - 포즈 리셋")]
    private void TestResetPose()
    {
        ResetPose();
    }

    [ContextMenu("테스트 - 50% 진행")]
    private void TestSetHalfProgress()
    {
        SetProgress(0.5f);
    }
}