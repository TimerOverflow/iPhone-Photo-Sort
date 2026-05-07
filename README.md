# 📸 iPhone Photo Sort

[![License: MIT](https://img.shields.io/badge/License-MIT-purple.svg)](https://opensource.org/licenses/MIT)
[![Build Status](https://img.shields.io/badge/Build-v1.0.0-blue.svg)]()

**iPhone Photo Sort** is an intelligent media organizer specifically designed for iPhone users. It automatically categorizes thousands of photos and videos by camera model and smartly matches iPhone-specific edited versions (Filtered) with their originals (Org).

![Hero Image](readme_hero.png)

---

## ✨ Key Features

- **🤖 Smart Auto-Classification**: Analyzes EXIF metadata to create folders based on camera models (e.g., iPhone 17 Pro, Galaxy S26, etc.) and moves files accordingly.
- **🍎 iPhone Optimized**: 
  - Recognizes `IMG_E...` edited versions and sorts them into a dedicated `Filtered` folder.
  - Automatically identifies corresponding original files and moves them to the `Org` folder.
  - Organizes `.AAE` sidecar files into a separate `AAE` folder.
- **📊 Comprehensive Metadata Extraction**: View Lens Model, Exposure Time, F-Number, ISO, Focal Length, Resolution, Video FPS, and GPS availability at a glance.
- **🌳 Interactive Folder Tree**: Once sorted, results are displayed in an intuitive tree structure. Hover over files to see detailed metadata popups.
- **💾 Session Persistence**: Generates a `.sort_history.json` file in the sorted directory, allowing you to instantly reload previous sorting results when reopening the folder.
- **🛡️ Conflict Resolution**: Choose between Overwrite, Skip, or Rename when encountering files with duplicate names.
- **🎨 Modern UI**: A sleek, premium dark-themed interface built with WPF.

---

## 🚀 Usage Guide

### 1. Launch the Application
Run `iPhone Photo Sort.exe`. You will be greeted by a modern, dark-themed user interface.

### 2. Select Source Folder
Click the **Folder Icon** button at the top to select the directory containing your media files. The file list will load asynchronously.

<img width="1173" height="671" alt="image" src="https://github.com/user-attachments/assets/b212a64b-1357-4b94-84de-41c6ed7358e9" />

### 3. Execute Sorting (SORT)
Click the large **SORT** button. The application will analyze metadata and move files in real-time. You can monitor progress via the status bar at the bottom.

<img width="1177" height="669" alt="image" src="https://github.com/user-attachments/assets/0c7fadca-72d0-4997-a1c5-dc0b4ec28976" />

### 4. Review Results
After sorting, the right-hand panel switches to the **Sorted Folder Tree**.
- Expand or collapse folders to review the organized structure.
- Hover over any file to view a **ToolTip** containing detailed metadata and the **Sorting Reason**.

<img width="442" height="537" alt="image" src="https://github.com/user-attachments/assets/521d32b2-6aed-49be-b8df-2df65068d0aa" />

---

## 🛠 Tech Stack

- **Language**: C# 
- **Framework**: .NET Framework (WPF)
- **Libraries**:
  - `MetadataExtractor`: For robust EXIF and QuickTime metadata parsing.
  - `WindowsAPICodePack`: For modern folder selection dialogs and shell integration.
  - `DataContractJsonSerializer`: For efficient session persistence and history management.

---

## ⚖️ License & Copyright

Copyright © TimerOverFlow. All rights reserved.
This project is licensed under the MIT License.

---

> [!TIP]
> iPhone edited photos (`IMG_E...`) should never be separated from their originals. **iPhone Photo Sort** ensures this link is preserved for a truly organized gallery.
