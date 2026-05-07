using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace iPhone_Photo_Sort
{
    /// <summary>
    /// 정렬 이력 전체를 저장/로드하기 위한 직렬화 가능한 데이터 클래스입니다.
    /// </summary>
    [DataContract]
    public class SortHistory
    {
        public const string FileName = ".sort_history.json";

        [DataMember] public string SourceDirectory { get; set; }
        [DataMember] public string SortedAt { get; set; }
        [DataMember] public string AppVersion { get; set; }
        [DataMember] public List<SortHistoryEntry> Entries { get; set; } = new List<SortHistoryEntry>();
    }

    /// <summary>
    /// 개별 파일의 정렬 이력 항목입니다.
    /// </summary>
    [DataContract]
    public class SortHistoryEntry
    {
        [DataMember] public string FileName { get; set; }
        [DataMember] public string FileSize { get; set; }

        // 메타데이터
        [DataMember] public string CameraModel { get; set; }
        [DataMember] public string Exposure { get; set; }
        [DataMember] public string FNumber { get; set; }
        [DataMember] public string ISO { get; set; }
        [DataMember] public string FocalLength { get; set; }
        [DataMember] public string DateTaken { get; set; }
        [DataMember] public string Resolution { get; set; }
        [DataMember] public string LensModel { get; set; }
        [DataMember] public string Location { get; set; }
        [DataMember] public string Duration { get; set; }
        [DataMember] public string FrameRate { get; set; }

        // 분류 결과
        [DataMember] public string DestinationFolder { get; set; }
        [DataMember] public string SortingReason { get; set; }

        /// <summary>
        /// SortHistoryEntry로부터 FileSortInfo를 복원합니다.
        /// </summary>
        public FileSortInfo ToFileSortInfo()
        {
            return new FileSortInfo
            {
                FileName = FileName,
                FileSize = FileSize,
                CameraModel = CameraModel,
                Exposure = Exposure,
                FNumber = FNumber,
                ISO = ISO,
                FocalLength = FocalLength,
                DateTaken = DateTaken,
                Resolution = Resolution,
                LensModel = LensModel,
                Location = Location,
                Duration = Duration,
                FrameRate = FrameRate,
                DestinationFolder = DestinationFolder,
                SortingReason = SortingReason,
                IsSorted = true
            };
        }

        /// <summary>
        /// FileSortInfo로부터 SortHistoryEntry를 생성합니다.
        /// </summary>
        public static SortHistoryEntry FromFileSortInfo(FileSortInfo info)
        {
            return new SortHistoryEntry
            {
                FileName = info.FileName,
                FileSize = info.FileSize,
                CameraModel = info.CameraModel,
                Exposure = info.Exposure,
                FNumber = info.FNumber,
                ISO = info.ISO,
                FocalLength = info.FocalLength,
                DateTaken = info.DateTaken,
                Resolution = info.Resolution,
                LensModel = info.LensModel,
                Location = info.Location,
                Duration = info.Duration,
                FrameRate = info.FrameRate,
                DestinationFolder = info.DestinationFolder,
                SortingReason = info.SortingReason
            };
        }
    }
}
