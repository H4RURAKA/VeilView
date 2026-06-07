# 빌드 메모

## 단일 exe

```powershell
.\BUILD_SINGLE_EXE.cmd
```

결과:

```text
dist\VeilView.exe
```

단일 exe는 .NET 런타임과 WebView2 Loader 의존 파일을 함께 묶는 방식입니다. 실행 PC에는 Microsoft Edge WebView2 Runtime이 필요합니다.

## 폴더 번들

```powershell
.\BUILD_PORTABLE_FOLDER.cmd
```

결과:

```text
dist-folder\
```

폴더 번들은 exe만 따로 옮기면 안 되고, 생성된 폴더 내부 파일을 함께 유지해야 합니다.
