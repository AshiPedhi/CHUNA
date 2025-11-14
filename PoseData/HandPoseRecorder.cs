using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Oculus.Interaction.Input;
using Oculus.Interaction;
using System.Text;
using System.Globalization;
using Cysharp.Threading.Tasks;

public class HandPoseRecorder : MonoBehaviour
{
    [Header("��ȭ�� �� ��")]
    [SerializeField]
    private HandVisual leftHandVisual;

    [SerializeField]
    private HandVisual rightHandVisual;

    [Header("OpenXR Root (�ڵ� Ž��)")]
    [SerializeField]
    private Transform leftOpenXRRoot;

    [SerializeField]
    private Transform rightOpenXRRoot;

    [Header("��ȭ ����")]
    [SerializeField]
    private string recordingFileName = "HandPose";

    [SerializeField]
    private float recordInterval = 0.1f;

    [SerializeField]
    private bool recordLeftHand = true;

    [SerializeField]
    private bool recordRightHand = true;

    [SerializeField]
    private Transform referencePoint;

    [Header("Ÿ�̸� ����")]
    [SerializeField]
    private bool useTimer = false;

    [SerializeField]
    private float timerDuration = 60f;

    [SerializeField]
    private bool showCountdown = true;

    [Header("��ȭ ����")]
    [SerializeField]
    private bool isRecording = false;

    [SerializeField]
    private bool isWaitingToRecord = false;

    [SerializeField]
    private int recordedFrames = 0;

    [SerializeField]
    private float remainingTime = 0f;

    private List<FrameData> recordedData = new List<FrameData>();
    private float lastRecordTime = 0f;
    private float recordingStartTime = 0f;
    private int currentFrameIndex = 0;

    private StringBuilder csvBuilder = new StringBuilder(1024 * 100);

    // Ÿ�̸� �̺�Ʈ
    public event Action<float> OnTimerTick;
    public event Action OnTimerComplete;
    public event Action OnRecordingStarted;
    public event Action OnRecordingStopped;

    [System.Serializable]
    private class FrameData
    {
        public int frameIndex;
        public string handType;
        public int jointId;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 rootPosition;     // OpenXRRoot�� ��ġ
        public Quaternion rootRotation;  // OpenXRRoot�� ȸ��
        public float timestamp;
    }

    void Start()
    {
        FindOpenXRRoots();

        if (referencePoint == null)
        {
            Debug.LogWarning("�������� �������� �ʾ� ���� ��ǥ�� ����մϴ�.");
        }
    }

    /// <summary>
    /// OpenXRRoot GameObject �ڵ� Ž��
    /// </summary>
    private void FindOpenXRRoots()
    {
        // �޼� OpenXRRoot ã��
        if (leftHandVisual != null && leftOpenXRRoot == null)
        {
            Transform parent = leftHandVisual.transform.parent;
            while (parent != null)
            {
                if (parent.name.Contains("OpenXRLeftHand") || parent.name.Contains("LeftHandAnchor"))
                {
                    leftOpenXRRoot = parent;
                    Debug.Log($"�޼� OpenXRRoot ã��: {leftOpenXRRoot.name}");
                    break;
                }
                parent = parent.parent;
            }

            if (leftOpenXRRoot == null)
            {
                Debug.LogWarning("�޼� OpenXRRoot�� ã�� �� �����ϴ�. ���� ���� �ʿ�!");
            }
        }

        // ������ OpenXRRoot ã��
        if (rightHandVisual != null && rightOpenXRRoot == null)
        {
            Transform parent = rightHandVisual.transform.parent;
            while (parent != null)
            {
                if (parent.name.Contains("OpenXRRightHand") || parent.name.Contains("RightHandAnchor"))
                {
                    rightOpenXRRoot = parent;
                    Debug.Log($"������ OpenXRRoot ã��: {rightOpenXRRoot.name}");
                    break;
                }
                parent = parent.parent;
            }

            if (rightOpenXRRoot == null)
            {
                Debug.LogWarning("������ OpenXRRoot�� ã�� �� �����ϴ�. ���� ���� �ʿ�!");
            }
        }
    }

    void Update()
    {
        if (isRecording)
        {
            if (Time.time - lastRecordTime >= recordInterval)
            {
                RecordFrame();
                lastRecordTime = Time.time;
            }

            // Ÿ�̸� ��� ���̸� �ð� üũ
            if (useTimer)
            {
                remainingTime = timerDuration - (Time.time - recordingStartTime);

                if (remainingTime <= 0)
                {
                    StopRecording();
                    OnTimerComplete?.Invoke();
                }
            }
        }

        // Ÿ�̸� ��� ��
        if (isWaitingToRecord)
        {
            remainingTime -= Time.deltaTime;

            if (showCountdown)
            {
                OnTimerTick?.Invoke(remainingTime);
            }

            if (remainingTime <= 0)
            {
                isWaitingToRecord = false;
                ActualStartRecording();
                OnTimerComplete?.Invoke();
            }
        }
    }

    // ��ȭ ���� (Ÿ�̸� ����)
    public void StartRecording()
    {
        if (isRecording || isWaitingToRecord)
        {
            Debug.LogWarning("�̹� ��ȭ ���̰ų� ��� ���Դϴ�.");
            return;
        }

        // OpenXRRoot ��Ž�� (��Ÿ�� �� ����� �� ����)
        FindOpenXRRoots();

        recordedData.Clear();
        recordedFrames = 0;
        currentFrameIndex = 0;

        if (useTimer && showCountdown)
        {
            // ī��Ʈ�ٿ� ����
            isWaitingToRecord = true;
            remainingTime = 3f; // 3�� ī��Ʈ�ٿ�
            Debug.Log("<color=orange>3�� �� ��ȭ�� �����մϴ�...</color>");
        }
        else
        {
            ActualStartRecording();
        }
    }

    // ���� ��ȭ ����
    private void ActualStartRecording()
    {
        isRecording = true;
        recordingStartTime = Time.time;
        lastRecordTime = Time.time;

        if (useTimer)
        {
            remainingTime = timerDuration;
        }

        Debug.Log($"<color=green>��ȭ ����!</color>\n" +
                 $"���ϸ�: {recordingFileName}.csv\n" +
                 $"��ȭ ����: {recordInterval}��\n" +
                 $"�޼�: {(recordLeftHand ? "ON" : "OFF")}\n" +
                 $"������: {(recordRightHand ? "ON" : "OFF")}\n" +
                 $"Ÿ�̸�: {(useTimer ? timerDuration + "��" : "OFF")}");

        OnRecordingStarted?.Invoke();
    }

    // Ÿ�̸� ���
    public void CancelTimer()
    {
        if (isWaitingToRecord)
        {
            isWaitingToRecord = false;
            remainingTime = 0f;
            Debug.Log("<color=red>Ÿ�̸� ��ҵ�</color>");
        }
    }

    // ��ȭ ����
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("��ȭ ���� �ƴմϴ�.");
            return;
        }

        isRecording = false;

        float recordingDuration = Time.time - recordingStartTime;
        Debug.Log($"<color=yellow>��ȭ ����</color>\n" +
                 $"��ȭ �ð�: {recordingDuration:F1}��\n" +
                 $"������ ��: {recordedFrames}\n" +
                 $"���� ��...");

        SaveToCSVAsync().Forget();

        OnRecordingStopped?.Invoke();
    }

    private void RecordFrame()
    {
        bool frameRecorded = false;

        if (recordLeftHand && leftHandVisual != null)
        {
            if (RecordHandData(leftHandVisual, "Left", leftOpenXRRoot))
            {
                frameRecorded = true;
            }
        }

        if (recordRightHand && rightHandVisual != null)
        {
            if (RecordHandData(rightHandVisual, "Right", rightOpenXRRoot))
            {
                frameRecorded = true;
            }
        }

        if (frameRecorded)
        {
            currentFrameIndex++;
            recordedFrames++;

            if (recordedFrames % 10 == 0)
            {
                Debug.Log($"��ȭ ��... ������: {recordedFrames}");
            }
        }
    }

    private bool RecordHandData(HandVisual handVisual, string handType, Transform openXRRoot)
    {
        if (handVisual == null || handVisual.Hand == null)
            return false;

        if (!handVisual.Hand.IsTrackedDataValid)
        {
            Debug.LogWarning($"{handType} �ڵ� Ʈ��ŷ �����Ͱ� ��ȿ���� �ʽ��ϴ�.");
            return false;
        }

        float timestamp = Time.time - recordingStartTime;

        // OpenXRRoot Transform ����
        Vector3 rootPos = Vector3.zero;
        Quaternion rootRot = Quaternion.identity;

        if (openXRRoot != null)
        {
            if (referencePoint != null)
            {
                // ������ ��� ��ǥ�� ����
                rootPos = openXRRoot.position - referencePoint.position;
                rootRot = Quaternion.Inverse(referencePoint.rotation) * openXRRoot.rotation;
            }
            else
            {
                // ���� ��ǥ�� ����
                rootPos = openXRRoot.position;
                rootRot = openXRRoot.rotation;
            }
        }
        else
        {
            Debug.LogWarning($"{handType} OpenXRRoot�� ã�� �� �����ϴ�!");
        }

        // �� ����Ʈ ������ ����
        for (int i = 0; i < handVisual.Joints.Count; i++)
        {
            Transform joint = handVisual.Joints[i];
            if (joint == null)
                continue;

            FrameData frameData = new FrameData
            {
                frameIndex = currentFrameIndex,
                handType = handType,
                jointId = i,
                localPosition = joint.localPosition,
                localRotation = joint.localRotation,
                timestamp = timestamp
            };

            // Wrist ����Ʈ���� Root Transform ����
            if (i == (int)HandJointId.HandWristRoot)
            {
                frameData.rootPosition = rootPos;
                frameData.rootRotation = rootRot;

                if (recordedFrames % 10 == 0)
                {
                    Debug.Log($"[Frame {currentFrameIndex}] {handType} Root Pos: {rootPos}, Rot: {rootRot.eulerAngles}");
                }
            }
            else
            {
                frameData.rootPosition = Vector3.zero;
                frameData.rootRotation = Quaternion.identity;
            }

            recordedData.Add(frameData);
        }

        return true;
    }

    /// <summary>
    /// CSV 파일로 비동기 저장
    /// </summary>
    private async UniTask SaveToCSVAsync()
    {
        if (recordedData.Count == 0)
        {
            Debug.LogError("[HandPoseRecorder] 저장할 데이터가 없습니다.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, recordingFileName + ".csv");

        try
        {
            // 백그라운드 스레드에서 CSV 생성
            await UniTask.SwitchToThreadPool();

            csvBuilder.Clear();

            // CSV 헤더 - RootPos/Rot 포함
            csvBuilder.AppendLine("FrameIndex,HandType,JointID,LocalPosX,LocalPosY,LocalPosZ," +
                                 "LocalRotX,LocalRotY,LocalRotZ,LocalRotW,Timestamp," +
                                 "RootPosX,RootPosY,RootPosZ,RootRotX,RootRotY,RootRotZ,RootRotW");

            CultureInfo invariantCulture = CultureInfo.InvariantCulture;

            foreach (FrameData data in recordedData)
            {
                if (data.jointId == (int)HandJointId.HandWristRoot)
                {
                    // Root Transform 정보 포함
                    csvBuilder.AppendFormat(invariantCulture,
                        "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}\n",
                        data.frameIndex,
                        data.handType,
                        data.jointId,
                        data.localPosition.x, data.localPosition.y, data.localPosition.z,
                        data.localRotation.x, data.localRotation.y, data.localRotation.z, data.localRotation.w,
                        data.timestamp,
                        data.rootPosition.x, data.rootPosition.y, data.rootPosition.z,
                        data.rootRotation.x, data.rootRotation.y, data.rootRotation.z, data.rootRotation.w
                    );
                }
                else
                {
                    // 다른 조인트는 Root 정보 없음
                    csvBuilder.AppendFormat(invariantCulture,
                        "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},,,,,,,\n",
                        data.frameIndex,
                        data.handType,
                        data.jointId,
                        data.localPosition.x, data.localPosition.y, data.localPosition.z,
                        data.localRotation.x, data.localRotation.y, data.localRotation.z, data.localRotation.w,
                        data.timestamp
                    );
                }
            }

            string csvContent = csvBuilder.ToString();

            // 메인 스레드로 복귀
            await UniTask.SwitchToMainThread();

            // 비동기 파일 쓰기
            await File.WriteAllTextAsync(path, csvContent);

            long fileSize = new FileInfo(path).Length;
            Debug.Log($"<color=green>[HandPoseRecorder] CSV 저장 완료!</color>\n" +
                     $"경로: {path}\n" +
                     $"크기: {fileSize / 1024f:F1} KB\n" +
                     $"프레임: {recordedData.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[HandPoseRecorder] CSV 저장 실패: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 동기 버전 래퍼 (하위 호환성)
    /// </summary>
    private void SaveToCSV()
    {
        SaveToCSVAsync().Forget();
    }

    // ��ȭ ���
    public void ToggleRecording()
    {
        if (isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    // ���ϸ� ����
    public void SetFileName(string fileName)
    {
        if (!string.IsNullOrEmpty(fileName))
        {
            recordingFileName = fileName;
        }
    }

    // ��ȭ ���� ����
    public void SetRecordInterval(float interval)
    {
        recordInterval = Mathf.Max(0.01f, interval);
    }

    // Ÿ�̸� ����
    public void SetTimerDuration(float duration)
    {
        timerDuration = Mathf.Max(1f, duration);
    }

    // Ÿ�̸� ���
    public void SetUseTimer(bool use)
    {
        useTimer = use;
    }

    // ���� Ȯ��
    public bool IsRecording() => isRecording;
    public bool IsWaitingToRecord() => isWaitingToRecord;
    public int GetRecordedFrames() => recordedFrames;
    public float GetRemainingTime() => remainingTime;
    public float GetRecordingDuration() => isRecording ? Time.time - recordingStartTime : 0f;

    // �� Ȱ��ȭ ���� ����
    public void SetRecordLeftHand(bool record)
    {
        recordLeftHand = record;
    }

    public void SetRecordRightHand(bool record)
    {
        recordRightHand = record;
    }

    public bool IsRecordingLeftHand() => recordLeftHand;
    public bool IsRecordingRightHand() => recordRightHand;

    // CSV ���� ��� ��������
    public string GetSavedFilePath()
    {
        return Path.Combine(Application.persistentDataPath, recordingFileName + ".csv");
    }

    // CSV ���� ���� Ȯ��
    public bool DoesSavedFileExist()
    {
        return File.Exists(GetSavedFilePath());
    }

    // OpenXRRoot ���� ����
    public void SetLeftOpenXRRoot(Transform root)
    {
        leftOpenXRRoot = root;
        Debug.Log($"�޼� OpenXRRoot ���� ����: {root?.name}");
    }

    public void SetRightOpenXRRoot(Transform root)
    {
        rightOpenXRRoot = root;
        Debug.Log($"������ OpenXRRoot ���� ����: {root?.name}");
    }
}