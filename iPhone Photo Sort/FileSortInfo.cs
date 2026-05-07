using System.ComponentModel;

namespace iPhone_Photo_Sort
{
    public class FileSortInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 기본 정보
        public string FileName { get; set; }
        public string OriginalPath { get; set; }
        public string FileSize { get; set; }

        // 메타데이터 (정렬 후 채워짐)
        public string CameraModel { get; set; }
        public string Exposure { get; set; }
        public string ShutterSpeed { get; set; }
        public string FNumber { get; set; }
        public string ISO { get; set; }
        public string FocalLength { get; set; }
        public string DateTaken { get; set; }
        public string Resolution { get; set; }
        public string LensModel { get; set; }
        public string Location { get; set; }
        public string Duration { get; set; }
        public string FrameRate { get; set; }

        // 분류 결과 (정렬 후 채워짐)
        private string _destinationFolder;
        public string DestinationFolder
        {
            get => _destinationFolder;
            set
            {
                _destinationFolder = value;
                OnPropertyChanged(nameof(DestinationFolder));
                OnPropertyChanged(nameof(MetadataTooltip));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusOpacity));
            }
        }

        private string _sortingReason;
        public string SortingReason
        {
            get => _sortingReason;
            set
            {
                _sortingReason = value;
                OnPropertyChanged(nameof(SortingReason));
                OnPropertyChanged(nameof(MetadataTooltip));
            }
        }

        private bool _isSorted;
        public bool IsSorted
        {
            get => _isSorted;
            set
            {
                _isSorted = value;
                OnPropertyChanged(nameof(IsSorted));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusOpacity));
                OnPropertyChanged(nameof(MetadataTooltip));
            }
        }

        // UI 표시용 프로퍼티
        public string StatusIcon => IsSorted ? "✅" : "⏳";

        /// <summary>
        /// SORT 전에는 반투명(0.45), SORT 후에는 불투명(1.0)으로 표시하여
        /// 사용자가 "아직 상세 정보를 볼 수 없다"는 것을 시각적으로 인지하도록 함
        /// </summary>
        public double StatusOpacity => IsSorted ? 1.0 : 0.45;

        public string MetadataTooltip
        {
            get
            {
                if (!IsSorted)
                    return "ℹ️ Run SORT to view detailed metadata.";

                string tooltip = 
                    $"📷 Camera: {CameraModel ?? "Unknown"}\n";

                if (!string.IsNullOrEmpty(LensModel))
                    tooltip += $"🔍 Lens: {LensModel}\n";

                tooltip += $"📐 Resolution: {Resolution ?? "N/A"}\n";

                if (!string.IsNullOrEmpty(Duration))
                    tooltip += $"⏱️ Duration: {Duration}\n";
                if (!string.IsNullOrEmpty(FrameRate))
                    tooltip += $"🎞️ Frame Rate: {FrameRate}\n";

                tooltip += 
                    $"⚡ Exposure: {Exposure ?? "N/A"}\n" +
                    $"🔆 F-Number: {FNumber ?? "N/A"}\n" +
                    $"🎞️ ISO: {ISO ?? "N/A"}\n" +
                    $"🔭 Focal Length: {FocalLength ?? "N/A"}\n" +
                    $"📅 Date Taken: {DateTaken ?? "N/A"}\n";

                if (!string.IsNullOrEmpty(Location))
                    tooltip += $"📍 Location: {Location}\n";

                tooltip += 
                    $"💾 File Size: {FileSize ?? "N/A"}\n" +
                    $"──────────────────\n" +
                    $"📂 Destination: {DestinationFolder}\n" +
                    $"📋 Reason: {SortingReason}";

                return tooltip;
            }
        }
    }
}
