# Development Rules
컴파일을 할 때는 UTF8로 컴파일 할 것.
예1) chcp 65001 >$null; dotnet build art.SmartIcon.sln
예2) dotnet build art.SmartIcon.sln --output-encoding utf-8
예3) dotnet test DynamicCrawler.sln --verbosity normal 2>&1 | Out-File d:\sources\github\Dynamic-Crawler\test_output.txt -Encoding