using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CHUNA.PoseData
{
    /// <summary>
    /// 포즈 데이터 CSV 파일 처리 유틸리티
    /// 비동기 로딩 및 저장 지원
    /// </summary>
    public static class PoseDataUtility
    {
        private const int INITIAL_STRING_BUILDER_CAPACITY = 1024 * 100; // 100KB

        /// <summary>
        /// CSV 파일에서 포즈 시퀀스를 비동기로 로드
        /// </summary>
        /// <param name="filePath">CSV 파일 전체 경로</param>
        /// <returns>로드된 PoseSequence</returns>
        public static async UniTask<PoseSequence> LoadFromCSVAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[PoseDataUtility] 파일을 찾을 수 없습니다: {filePath}");
                return null;
            }

            try
            {
                // 비동기 파일 읽기
                string csvContent = await File.ReadAllTextAsync(filePath);

                // 백그라운드 스레드에서 파싱
                await UniTask.SwitchToThreadPool();
                PoseSequence sequence = ParseCSVContent(csvContent, Path.GetFileNameWithoutExtension(filePath));

                // 메인 스레드로 복귀
                await UniTask.SwitchToMainThread();

                Debug.Log($"<color=green>[PoseDataUtility] 로드 완료: {sequence}</color>");
                return sequence;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PoseDataUtility] CSV 로드 실패: {filePath}\n{e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 포즈 시퀀스를 CSV 파일로 비동기 저장
        /// </summary>
        /// <param name="sequence">저장할 PoseSequence</param>
        /// <param name="filePath">저장할 파일 경로</param>
        public static async UniTask<bool> SaveToCSVAsync(PoseSequence sequence, string filePath)
        {
            if (sequence == null || sequence.frames.Count == 0)
            {
                Debug.LogError("[PoseDataUtility] 저장할 데이터가 없습니다.");
                return false;
            }

            try
            {
                // 백그라운드 스레드에서 CSV 생성
                await UniTask.SwitchToThreadPool();
                string csvContent = GenerateCSVContent(sequence);

                // 메인 스레드로 복귀
                await UniTask.SwitchToMainThread();

                // 디렉토리 확인/생성
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 비동기 파일 쓰기
                await File.WriteAllTextAsync(filePath, csvContent, Encoding.UTF8);

                Debug.Log($"<color=green>[PoseDataUtility] CSV 저장 완료: {filePath}</color>");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PoseDataUtility] CSV 저장 실패: {filePath}\n{e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// CSV 내용 파싱 (동기)
        /// </summary>
        private static PoseSequence ParseCSVContent(string csvContent, string fileName)
        {
            PoseSequence sequence = new PoseSequence { fileName = fileName };

            string[] lines = csvContent.Split('\n');
            if (lines.Length < 2)
            {
                Debug.LogWarning("[PoseDataUtility] CSV 데이터가 부족합니다.");
                return sequence;
            }

            // 프레임별로 데이터 그룹화
            Dictionary<int, PoseFrame> frameDict = new Dictionary<int, PoseFrame>();

            // 헤더 스킵 (라인 0)
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(',');
                if (values.Length < 11) continue; // 최소 필드 수

                try
                {
                    int frameIndex = int.Parse(values[0].Trim());
                    string handType = values[1].Trim();
                    int jointId = int.Parse(values[2].Trim());

                    // 프레임 생성 또는 가져오기
                    if (!frameDict.ContainsKey(frameIndex))
                    {
                        frameDict[frameIndex] = new PoseFrame
                        {
                            frameIndex = frameIndex,
                            timestamp = float.Parse(values[10].Trim(), CultureInfo.InvariantCulture)
                        };
                    }

                    PoseFrame frame = frameDict[frameIndex];

                    // 로컬 포즈 데이터
                    Vector3 localPos = new Vector3(
                        float.Parse(values[3].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(values[4].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(values[5].Trim(), CultureInfo.InvariantCulture)
                    );

                    Quaternion localRot = new Quaternion(
                        float.Parse(values[6].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(values[7].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(values[8].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(values[9].Trim(), CultureInfo.InvariantCulture)
                    );

                    PoseData poseData = new PoseData(localPos, localRot);

                    // 손 타입에 따라 저장
                    if (handType == "Left" || handType == "left")
                    {
                        frame.leftLocalPoses[jointId] = poseData;
                    }
                    else if (handType == "Right" || handType == "right")
                    {
                        frame.rightLocalPoses[jointId] = poseData;
                    }

                    // 월드 좌표 (선택적, 11 이후 필드)
                    if (values.Length >= 18)
                    {
                        Vector3 worldPos = new Vector3(
                            float.Parse(values[11].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(values[12].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(values[13].Trim(), CultureInfo.InvariantCulture)
                        );

                        Quaternion worldRot = new Quaternion(
                            float.Parse(values[14].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(values[15].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(values[16].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(values[17].Trim(), CultureInfo.InvariantCulture)
                        );

                        // 첫 번째 조인트(보통 손목)를 손 전체 위치로 사용
                        if (jointId == 0)
                        {
                            if (handType == "Left" || handType == "left")
                            {
                                frame.leftHandWorldPosition = worldPos;
                                frame.leftHandWorldRotation = worldRot;
                            }
                            else if (handType == "Right" || handType == "right")
                            {
                                frame.rightHandWorldPosition = worldPos;
                                frame.rightHandWorldRotation = worldRot;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PoseDataUtility] 라인 {i} 파싱 오류: {e.Message}");
                }
            }

            // 프레임 인덱스 순으로 정렬
            var sortedFrames = new List<int>(frameDict.Keys);
            sortedFrames.Sort();

            foreach (int frameIndex in sortedFrames)
            {
                sequence.frames.Add(frameDict[frameIndex]);
            }

            // 메타데이터 업데이트
            sequence.UpdateMetadata();

            return sequence;
        }

        /// <summary>
        /// CSV 내용 생성 (동기)
        /// </summary>
        private static string GenerateCSVContent(PoseSequence sequence)
        {
            StringBuilder sb = new StringBuilder(INITIAL_STRING_BUILDER_CAPACITY);

            // 헤더 작성
            sb.AppendLine(CSVHeaders.GetHeaderLine());

            // 프레임별 데이터 작성
            foreach (PoseFrame frame in sequence.frames)
            {
                // 왼손 데이터
                if (frame.leftLocalPoses.Count > 0)
                {
                    WriteHandData(sb, frame, HandType.Left);
                }

                // 오른손 데이터
                if (frame.rightLocalPoses.Count > 0)
                {
                    WriteHandData(sb, frame, HandType.Right);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 특정 손의 데이터를 CSV에 작성
        /// </summary>
        private static void WriteHandData(StringBuilder sb, PoseFrame frame, HandType handType)
        {
            var poses = frame.GetHandPoses(handType);
            Vector3 worldPos = frame.GetHandWorldPosition(handType);
            Quaternion worldRot = frame.GetHandWorldRotation(handType);

            string handTypeStr = handType == HandType.Left ? "Left" : "Right";

            foreach (var kvp in poses)
            {
                int jointId = kvp.Key;
                PoseData pose = kvp.Value;

                sb.Append($"{frame.frameIndex},");
                sb.Append($"{handTypeStr},");
                sb.Append($"{jointId},");
                sb.Append($"{pose.position.x:F6},{pose.position.y:F6},{pose.position.z:F6},");
                sb.Append($"{pose.rotation.x:F6},{pose.rotation.y:F6},{pose.rotation.z:F6},{pose.rotation.w:F6},");
                sb.Append($"{frame.timestamp:F4},");

                // 월드 좌표 (첫 번째 조인트만 유효, 나머지는 0)
                if (jointId == 0)
                {
                    sb.Append($"{worldPos.x:F6},{worldPos.y:F6},{worldPos.z:F6},");
                    sb.Append($"{worldRot.x:F6},{worldRot.y:F6},{worldRot.z:F6},{worldRot.w:F6}");
                }
                else
                {
                    sb.Append("0,0,0,0,0,0,1");
                }

                sb.AppendLine();
            }
        }

        /// <summary>
        /// CSV 파일 유효성 검사
        /// </summary>
        public static bool ValidateCSVFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[PoseDataUtility] 파일이 존재하지 않습니다: {filePath}");
                return false;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length < 2)
                {
                    Debug.LogWarning("[PoseDataUtility] CSV 파일이 비어있거나 헤더만 있습니다.");
                    return false;
                }

                // 헤더 확인
                string header = lines[0];
                if (!header.Contains("FrameIndex") || !header.Contains("HandType") || !header.Contains("JointID"))
                {
                    Debug.LogWarning("[PoseDataUtility] CSV 헤더 형식이 올바르지 않습니다.");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PoseDataUtility] CSV 검증 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 기본 저장 경로 가져오기
        /// </summary>
        public static string GetDefaultSavePath(string fileName)
        {
            string directory = Path.Combine(Application.persistentDataPath, "HandPoseData");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(directory, $"{fileName}_{timestamp}.csv");
        }

        /// <summary>
        /// 특정 디렉토리의 모든 CSV 파일 나열
        /// </summary>
        public static List<string> GetAllCSVFiles(string directory = null)
        {
            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(Application.persistentDataPath, "HandPoseData");
            }

            List<string> csvFiles = new List<string>();

            if (!Directory.Exists(directory))
            {
                return csvFiles;
            }

            string[] files = Directory.GetFiles(directory, "*.csv");
            csvFiles.AddRange(files);

            return csvFiles;
        }
    }
}
