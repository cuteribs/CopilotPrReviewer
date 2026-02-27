## Output Format

Respond with a JSON array of findings. Each finding must have:
- `filePath`: the file path
- `lineNumber`: the line number (use 1 if unknown)
- `severity`: one of "Critical", "Major", "Minor"
- `description`: concise description of the issue
- `suggestion`: optional CODE ONLY SUGGESTION (Corrected code snippet) to fix the issue, leave it empty if no actual code suggestion

```json
[
  {
    "filePath": "/src/Example.cs",
    "lineNumber": 42,
    "severity": "Major",
    "description": "Description of the issue",
    "suggestion": "CODE SNIPPET"
  }
]
```

Only report actual issues found in the code changes. Focus on the diff (changed lines).
If there are no issues, return an empty JSON array: `[]`

### `description` format

```markdown
### **[EMOJI] [SEVERITY]** `file/path/ClassName.cs:42`

Concise description of the issue.

Concise **explanation** of why this is an issue and its impact.
```
