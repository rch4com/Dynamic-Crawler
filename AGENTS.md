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