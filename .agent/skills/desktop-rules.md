# Development Rules
컴파일을 할 때는 UTF8로 컴파일 할 것.
예1) chcp 65001 >$null; dotnet build art.SmartIcon.sln
예2) dotnet build art.SmartIcon.sln --output-encoding utf-8
예3) dotnet test DynamicCrawler.sln --verbosity normal 2>&1 | Out-File d:\sources\github\Dynamic-Crawler\test_output.txt -Encoding


## Commit message rules
When creating git commit messages, always use this format:

<prefix>: <short summary>

<detailed description>

### Allowed prefixes
- feat
- fix
- refactor
- chore
- docs
- style
- test
- perf
- ci
- build

### Rules
- Summary must be concise and written in Korean
- Keep the summary within about 60 characters
- Add a blank line after the summary
- Body should explain what changed and why
- Use bullet points in the body when there are multiple changes
- Never create a one-line commit message unless explicitly requested
- If the change does not fit a business feature or bug, prefer `chore`

**[CRITICAL INSTRUCTION FOR AI AGENT]**
When you (the AI Assistant) automatically generate or propose a commit message (e.g., using "Generating" features or auto-committing), you MUST strictly adhere to the prefix and Korean summary format defined above. Do NOT use default English summaries like "Update desktop-rules.md". Always read this `Commit message rules` section before proposing any git commit command.