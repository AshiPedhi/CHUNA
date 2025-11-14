using UnityEngine;
using CHUNA.Patient;

/// <summary>
/// 유사도에 따라 환자 모델의 움직임을 제어하는 컨트롤러 (리팩토링 버전)
/// HandPosePlayer의 유사도를 받아서 환자 모델을 진행시킴
///
/// 이 클래스는 다음과 같이 분할되었습니다:
/// - PatientJointMapper: 조인트 자동 매핑
/// - PatientAnimationHandler: 애니메이션 처리
/// - PatientModelController: 메인 제어 로직 (이 파일)
/// </summary>
public class PatientModelController : MonoBehaviour
{
    [Header("=== 환자 모델 설정 ===")]
    [SerializeField] private GameObject patientModel;
    [SerializeField] private Animator patientAnimator;

    [Header("=== 조인트 참조 (자동 매핑 시 선택사항) ===")]
    [SerializeField] private Transform patientSpine;
    [SerializeField] private Transform patientNeck;
    [SerializeField] private Transform patientHead;
    [SerializeField] private Transform patientChest;
    [SerializeField] private Transform patientUpperChest;
    [SerializeField] private Transform patientLeftShoulder;
    [SerializeField] private Transform patientLeftUpperArm;
    [SerializeField] private Transform patientLeftLowerArm;
    [SerializeField] private Transform patientLeftHand;
    [SerializeField] private Transform patientRightShoulder;
    [SerializeField] private Transform patientRightUpperArm;
    [SerializeField] private Transform patientRightLowerArm;
    [SerializeField] private Transform patientRightHand;

    [Header("=== 자동 매핑 설정 ===")]
    [SerializeField] private bool autoMapOnStart = true;
    [SerializeField] private bool useAnimator = true;
    [SerializeField] private bool useFallbackNaming = true;

    [Header("=== HandPosePlayer 연결 ===")]
    [SerializeField] private HandPosePlayer handPosePlayer;

    [Header("=== 진행 설정 ===")]
    [SerializeField] private float similarityThreshold = 0.7f;
    [SerializeField] private float requiredHoldTime = 2.0f;
    [SerializeField] private float progressSpeed = 0.5f;
    [SerializeField] private bool requireBothHands = true;

    [Header("=== 움직임 설정 ===")]
    [SerializeField] private bool useAnimationCurve = true;
    [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float maxRotation = 30f;
    [SerializeField] private float maxDisplacement = 0.1f;

    [Header("=== 팔 움직임 설정 ===")]
    [SerializeField] private float maxArmRaise = 60f;
    [SerializeField] private float maxElbowBend = 90f;
    [SerializeField] private bool enableShoulderRotation = true;
    [SerializeField] private bool enableElbowBend = true;
    [SerializeField] private bool moveBothArms = true;

    [Header("=== 시각적 피드백 ===")]
    [SerializeField] private bool showProgressIndicator = true;
    [SerializeField] private Color lowProgressColor = Color.red;
    [SerializeField] private Color midProgressColor = Color.yellow;
    [SerializeField] private Color highProgressColor = Color.green;

    // 헬퍼 클래스들
    private PatientJointMapper jointMapper;
    private PatientAnimationHandler animationHandler;

    // 상태 변수
    private float currentProgress = 0f;
    private float similarityHoldTime = 0f;
    private bool isProgressing = false;
    private bool isCompleted = false;

    // 공개 프로퍼티 (하위 호환성)
    public float CurrentProgress => currentProgress;
    public bool IsCompleted => isCompleted;
    public bool IsProgressing => isProgressing;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        if (autoMapOnStart)
        {
            AutoMapJoints();
        }

        // 애니메이션 핸들러 생성 및 초기 포즈 저장
        if (jointMapper != null)
        {
            animationHandler = new PatientAnimationHandler(
                jointMapper,
                progressCurve,
                useAnimationCurve,
                maxRotation,
                maxDisplacement,
                maxArmRaise,
                maxElbowBend,
                enableShoulderRotation,
                enableElbowBend,
                moveBothArms
            );

            animationHandler.SaveInitialPose();
        }
    }

    private void InitializeComponents()
    {
        if (patientModel == null)
        {
            patientModel = gameObject;
            Debug.LogWarning("[PatientModelController] patientModel이 할당되지 않아 자신을 사용합니다.");
        }

        if (patientAnimator == null && useAnimator)
        {
            patientAnimator = patientModel.GetComponent<Animator>();
            if (patientAnimator == null)
            {
                Debug.LogWarning("[PatientModelController] Animator를 찾을 수 없습니다. 이름 기반 매핑을 사용합니다.");
            }
        }

        if (handPosePlayer == null)
        {
            handPosePlayer = FindObjectOfType<HandPosePlayer>();
            if (handPosePlayer == null)
            {
                Debug.LogError("[PatientModelController] HandPosePlayer를 찾을 수 없습니다!");
            }
        }
    }

    /// <summary>
    /// 자동 조인트 매핑
    /// </summary>
    private void AutoMapJoints()
    {
        jointMapper = new PatientJointMapper(
            patientModel,
            patientAnimator,
            useAnimator,
            useFallbackNaming
        );

        bool success = jointMapper.AutoMapJoints();

        if (success)
        {
            // 매핑된 조인트를 Inspector 필드에 반영
            patientSpine = jointMapper.Spine;
            patientNeck = jointMapper.Neck;
            patientHead = jointMapper.Head;
            patientChest = jointMapper.Chest;
            patientUpperChest = jointMapper.UpperChest;
            patientLeftShoulder = jointMapper.LeftShoulder;
            patientLeftUpperArm = jointMapper.LeftUpperArm;
            patientLeftLowerArm = jointMapper.LeftLowerArm;
            patientLeftHand = jointMapper.LeftHand;
            patientRightShoulder = jointMapper.RightShoulder;
            patientRightUpperArm = jointMapper.RightUpperArm;
            patientRightLowerArm = jointMapper.RightLowerArm;
            patientRightHand = jointMapper.RightHand;
        }
        else if (patientSpine != null)
        {
            // 수동 할당된 조인트 사용
            jointMapper.SetJoints(
                patientSpine,
                patientNeck,
                patientHead,
                patientChest,
                patientUpperChest,
                patientLeftShoulder,
                patientLeftUpperArm,
                patientLeftLowerArm,
                patientLeftHand,
                patientRightShoulder,
                patientRightUpperArm,
                patientRightLowerArm,
                patientRightHand
            );
        }
    }

    private void Update()
    {
        if (!isProgressing || isCompleted) return;

        UpdateSimilarityCheck();

        if (animationHandler != null)
        {
            animationHandler.UpdatePatientPose(currentProgress);
        }
    }

    /// <summary>
    /// 유사도 체크 및 진행도 업데이트
    /// </summary>
    private void UpdateSimilarityCheck()
    {
        if (handPosePlayer == null) return;

        float leftSimilarity = handPosePlayer.GetLeftSimilarity();
        float rightSimilarity = handPosePlayer.GetRightSimilarity();

        bool leftPassed = handPosePlayer.IsLeftHandSimilar();
        bool rightPassed = handPosePlayer.IsRightHandSimilar();

        // 양손 모두 필요한 경우
        bool passed = requireBothHands ?
            (leftPassed && rightPassed) :
            (leftPassed || rightPassed);

        float avgSimilarity = requireBothHands ?
            (leftSimilarity + rightSimilarity) / 2f :
            Mathf.Max(leftSimilarity, rightSimilarity);

        // 임계값 이상이면 Hold Time 증가
        if (passed && avgSimilarity >= similarityThreshold)
        {
            similarityHoldTime += Time.deltaTime;

            // 필요한 시간만큼 유지되면 진행
            if (similarityHoldTime >= requiredHoldTime)
            {
                currentProgress += progressSpeed * Time.deltaTime;
                currentProgress = Mathf.Clamp01(currentProgress);

                if (currentProgress >= 1f && !isCompleted)
                {
                    CompleteMovement();
                }
            }
        }
        else
        {
            // 유사도 떨어지면 Hold Time 감소
            similarityHoldTime -= Time.deltaTime * 2f;
            similarityHoldTime = Mathf.Max(0f, similarityHoldTime);
        }
    }

    /// <summary>
    /// 움직임 완료 처리
    /// </summary>
    private void CompleteMovement()
    {
        isCompleted = true;
        Debug.Log("<color=green>[PatientModelController] 움직임 완료!</color>");
    }

    // ===== 공개 API (하위 호환성 유지) =====

    public void StartProgression()
    {
        isProgressing = true;
        isCompleted = false;
        currentProgress = 0f;
        similarityHoldTime = 0f;

        Debug.Log("[PatientModelController] 진행 시작");
    }

    public void StopProgression()
    {
        isProgressing = false;
        Debug.Log("[PatientModelController] 진행 중지");
    }

    public void ResetPose()
    {
        currentProgress = 0f;
        similarityHoldTime = 0f;
        isCompleted = false;

        if (animationHandler != null)
        {
            animationHandler.ResetToInitialPose();
        }

        Debug.Log("[PatientModelController] 포즈 초기화");
    }

    public void SetThreshold(float threshold)
    {
        similarityThreshold = Mathf.Clamp01(threshold);
        Debug.Log($"[PatientModelController] 임계값 변경: {similarityThreshold:F2}");
    }

    public void SetHoldTime(float time)
    {
        requiredHoldTime = Mathf.Max(0f, time);
        Debug.Log($"[PatientModelController] 유지 시간 변경: {requiredHoldTime:F1}초");
    }

    public void SetProgressSpeed(float speed)
    {
        progressSpeed = Mathf.Max(0f, speed);
        Debug.Log($"[PatientModelController] 진행 속도 변경: {progressSpeed:F2}");
    }

    public void SetRequireBothHands(bool require)
    {
        requireBothHands = require;
        Debug.Log($"[PatientModelController] 양손 필요 여부: {requireBothHands}");
    }

    public void SetArmMovement(float armRaise, float elbowBend)
    {
        maxArmRaise = armRaise;
        maxElbowBend = elbowBend;

        if (animationHandler != null)
        {
            animationHandler.UpdateSettings(
                maxArmRaise: maxArmRaise,
                maxElbowBend: maxElbowBend
            );
        }
    }

    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);

        if (animationHandler != null)
        {
            animationHandler.UpdatePatientPose(currentProgress);
        }
    }

    // 에디터 전용 메서드
#if UNITY_EDITOR
    [ContextMenu("수동 조인트 매핑")]
    private void ManualAutoMap()
    {
        AutoMapJoints();
    }

    [ContextMenu("매핑 상태 확인")]
    private void CheckMappingStatus()
    {
        if (jointMapper != null)
        {
            jointMapper.ValidateMapping();
        }
        else
        {
            Debug.LogWarning("[PatientModelController] JointMapper가 초기화되지 않았습니다.");
        }
    }
#endif
}
