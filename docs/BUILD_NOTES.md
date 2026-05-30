# 빌드 메모

## 단일 실행 파일 빌드

`BUILD_SINGLE_EXE.cmd`는 내부적으로 다음 명령을 실행합니다.

```powershell
.\build.ps1
```

핵심 MSBuild 속성은 다음과 같습니다.

```xml
<PublishSingleFile>true</PublishSingleFile>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
<SelfContained>true</SelfContained>
```

WebView2는 네이티브 로더 DLL을 사용하므로 `IncludeNativeLibrariesForSelfExtract`가 필요합니다.
이 옵션을 사용하면 `VeilView.exe` 하나만 다른 위치로 옮겨도 실행할 수 있는 형태로 배포할 수 있습니다.

## 폴더형 빌드

`BUILD_PORTABLE_FOLDER.cmd`는 내부적으로 다음 명령을 실행합니다.

```powershell
.\build.ps1 -FolderBundle
```

특정 PC에서 단일 실행 파일의 네이티브 DLL 추출 방식에 문제가 있을 때 사용합니다.
이 방식은 `dist-folder` 안의 파일을 모두 함께 유지해야 합니다.

## 빌드 결과물

| 방식 | 결과물 | 특징 |
| --- | --- | --- |
| 단일 실행 파일 | `dist\VeilView.exe` | 배포 권장 방식. exe 하나만 이동 가능 |
| 폴더형 | `dist-folder\` | 호환성 우선. 폴더 안 파일을 모두 함께 보관 필요 |
