using NUnit.Framework;
using UnityEngine;
using CHUNA.PoseData;
using Cysharp.Threading.Tasks;
using System.IO;

namespace CHUNA.Tests
{
    /// <summary>
    /// PoseDataUtility의 단위 테스트
    /// </summary>
    public class PoseDataUtilityTests
    {
        private string testFilePath;
        private PoseSequence testSequence;

        [SetUp]
        public void Setup()
        {
            // 테스트 데이터 준비
            testFilePath = Path.Combine(Application.temporaryCachePath, "test_pose.csv");

            testSequence = new PoseSequence
            {
                fileName = "test_pose"
            };

            // 테스트 프레임 생성
            var frame1 = new PoseFrame
            {
                frameIndex = 0,
                timestamp = 0f
            };
            frame1.leftLocalPoses[0] = new PoseData(Vector3.zero, Quaternion.identity);
            frame1.rightLocalPoses[0] = new PoseData(Vector3.one, Quaternion.identity);

            var frame2 = new PoseFrame
            {
                frameIndex = 1,
                timestamp = 0.1f
            };
            frame2.leftLocalPoses[0] = new PoseData(Vector3.up, Quaternion.identity);
            frame2.rightLocalPoses[0] = new PoseData(Vector3.down, Quaternion.identity);

            testSequence.frames.Add(frame1);
            testSequence.frames.Add(frame2);
            testSequence.UpdateMetadata();
        }

        [TearDown]
        public void TearDown()
        {
            // 테스트 파일 정리
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }

        [Test]
        public async void TestSaveAndLoadCSV()
        {
            // CSV 저장
            bool saveResult = await PoseDataUtility.SaveToCSVAsync(testSequence, testFilePath);
            Assert.IsTrue(saveResult, "CSV 저장 실패");
            Assert.IsTrue(File.Exists(testFilePath), "CSV 파일이 생성되지 않음");

            // CSV 로딩
            var loadedSequence = await PoseDataUtility.LoadFromCSVAsync(testFilePath);
            Assert.IsNotNull(loadedSequence, "CSV 로딩 실패");
            Assert.AreEqual(testSequence.totalFrames, loadedSequence.totalFrames, "프레임 수 불일치");
            Assert.AreEqual(testSequence.hasLeftHand, loadedSequence.hasLeftHand, "왼손 데이터 플래그 불일치");
            Assert.AreEqual(testSequence.hasRightHand, loadedSequence.hasRightHand, "오른손 데이터 플래그 불일치");
        }

        [Test]
        public void TestCSVValidation()
        {
            // 존재하지 않는 파일
            bool result1 = PoseDataUtility.ValidateCSVFile("nonexistent.csv");
            Assert.IsFalse(result1, "존재하지 않는 파일이 유효하다고 판정됨");
        }

        [Test]
        public void TestPoseSequenceMetadata()
        {
            Assert.AreEqual(2, testSequence.totalFrames, "프레임 수 불일치");
            Assert.AreEqual(0.1f, testSequence.totalDuration, "총 시간 불일치");
            Assert.IsTrue(testSequence.hasLeftHand, "왼손 데이터 플래그 오류");
            Assert.IsTrue(testSequence.hasRightHand, "오른손 데이터 플래그 오류");
        }

        [Test]
        public void TestPoseFrameGetters()
        {
            var frame = testSequence.frames[0];

            // HandType.Left 테스트
            var leftPoses = frame.GetHandPoses(HandType.Left);
            Assert.IsNotNull(leftPoses, "왼손 포즈 null");
            Assert.AreEqual(1, leftPoses.Count, "왼손 조인트 수 불일치");

            // HandType.Right 테스트
            var rightPoses = frame.GetHandPoses(HandType.Right);
            Assert.IsNotNull(rightPoses, "오른손 포즈 null");
            Assert.AreEqual(1, rightPoses.Count, "오른손 조인트 수 불일치");

            // 월드 위치 테스트
            Vector3 leftPos = frame.GetHandWorldPosition(HandType.Left);
            Assert.AreEqual(Vector3.zero, leftPos, "왼손 월드 위치 불일치");
        }

        [Test]
        public void TestPoseDataClone()
        {
            var original = new PoseData(Vector3.one, Quaternion.identity);
            var cloned = original.Clone();

            Assert.AreEqual(original.position, cloned.position, "위치 복사 실패");
            Assert.AreEqual(original.rotation, cloned.rotation, "회전 복사 실패");
            Assert.AreNotSame(original, cloned, "얕은 복사 (참조가 같음)");
        }

        [Test]
        public void TestPoseFrameClone()
        {
            var original = testSequence.frames[0];
            var cloned = original.Clone();

            Assert.AreEqual(original.frameIndex, cloned.frameIndex, "프레임 인덱스 불일치");
            Assert.AreEqual(original.timestamp, cloned.timestamp, "타임스탬프 불일치");
            Assert.AreNotSame(original, cloned, "얕은 복사 (참조가 같음)");
            Assert.AreNotSame(original.leftLocalPoses, cloned.leftLocalPoses, "딕셔너리 얕은 복사");
        }

        [Test]
        public void TestCSVHeaders()
        {
            string headerLine = CSVHeaders.GetHeaderLine();

            Assert.IsNotNull(headerLine, "헤더 라인 null");
            Assert.IsTrue(headerLine.Contains("FrameIndex"), "FrameIndex 헤더 누락");
            Assert.IsTrue(headerLine.Contains("HandType"), "HandType 헤더 누락");
            Assert.IsTrue(headerLine.Contains("JointID"), "JointID 헤더 누락");
            Assert.IsTrue(headerLine.Contains("Timestamp"), "Timestamp 헤더 누락");
        }
    }
}
