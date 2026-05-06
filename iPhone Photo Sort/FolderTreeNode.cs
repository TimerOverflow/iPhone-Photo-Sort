using System.Collections.ObjectModel;
using System.ComponentModel;

namespace iPhone_Photo_Sort
{
    /// <summary>
    /// 폴더 트리 구조의 노드를 나타냅니다.
    /// 폴더 노드와 파일 노드 모두를 표현할 수 있습니다.
    /// </summary>
    public class FolderTreeNode : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Name { get; set; }
        public string Icon => IsFolder ? "📁" : "📄";
        public bool IsFolder { get; set; }
        public FileSortInfo FileInfo { get; set; }
        public ObservableCollection<FolderTreeNode> Children { get; set; } = new ObservableCollection<FolderTreeNode>();

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        /// <summary>
        /// 폴더 노드이면 자식 파일 수, 파일 노드이면 메타데이터 ToolTip을 반환합니다.
        /// </summary>
        public string ToolTipText
        {
            get
            {
                if (IsFolder)
                {
                    int count = CountFiles(this);
                    return $"{Name} ({count} files)";
                }
                return FileInfo?.MetadataTooltip ?? "";
            }
        }

        /// <summary>
        /// 표시 이름: 폴더 노드이면 "폴더명 (N)", 파일 노드이면 파일명
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (IsFolder)
                {
                    int count = CountFiles(this);
                    return $"{Name} ({count})";
                }
                return Name;
            }
        }

        private static int CountFiles(FolderTreeNode node)
        {
            int count = 0;
            foreach (var child in node.Children)
            {
                if (child.IsFolder)
                    count += CountFiles(child);
                else
                    count++;
            }
            return count;
        }
    }
}
