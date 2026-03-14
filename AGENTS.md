# AGENTS.md

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