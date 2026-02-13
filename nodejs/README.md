# Copilot PR Reviewer

AI-powered pull request code reviewer for Azure DevOps using GitHub Copilot SDK.

[![CI](https://github.com/yourusername/copilot-pr-reviewer/actions/workflows/ci.yml/badge.svg)](https://github.com/yourusername/copilot-pr-reviewer/actions/workflows/ci.yml)
[![NPM Version](https://img.shields.io/npm/v/copilot-pr-reviewer.svg)](https://www.npmjs.com/package/copilot-pr-reviewer)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

✨ **AI-Powered Reviews** - Leverages GitHub Copilot SDK for intelligent code analysis  
🎯 **Multi-Language Support** - Reviews .NET, Python, Frontend (JS/TS/React), and config files  
📊 **Batch Processing** - Efficiently handles large PRs with smart batching  
🔍 **Severity Classification** - Categorizes findings as Critical, Major, or Minor  
💬 **Azure DevOps Integration** - Posts review comments directly to PRs  
⚡ **Parallel Processing** - Reviews multiple batches concurrently  
📋 **Customizable Guidelines** - Use custom review guidelines for your project

## Installation

### Using npx (Recommended)

No installation needed! Run directly:

```bash
npx copilot-pr-reviewer <PR_URL>
```

### Global Installation

```bash
npm install -g copilot-pr-reviewer
```

### Local Project Installation

```bash
npm install --save-dev copilot-pr-reviewer
```

## Prerequisites

1. **GitHub Token** - For Copilot API access
   - Set via environment variable: `GITHUB_TOKEN=ghp_xxx`
   - Or pass via CLI flag: `--github-token ghp_xxx`

2. **Azure DevOps PAT** (Personal Access Token)
   - Set via environment variable: `AZURE_DEVOPS_PAT=xxx`
   - Or pass via CLI flag: `--pat xxx`

## Usage

### Basic Usage

```bash
# Using npx
npx copilot-pr-reviewer "https://dev.azure.com/org/project/_git/repo/pullrequest/123"

# With environment variables
export GITHUB_TOKEN="ghp_your_github_token"
export AZURE_DEVOPS_PAT="your_ado_pat"
npx copilot-pr-reviewer <PR_URL>

# With CLI flags
npx copilot-pr-reviewer <PR_URL> --github-token ghp_xxx --pat ado_xxx
```

### Advanced Usage

```bash
# Dry run (no comments posted)
npx copilot-pr-reviewer <PR_URL> --no-comments

# Custom AI model
npx copilot-pr-reviewer <PR_URL> --model gpt-4

# Custom guidelines
npx copilot-pr-reviewer <PR_URL> --guidelines-path ./my-guidelines

# Adjust parallelism
npx copilot-pr-reviewer <PR_URL> --max-parallel 4

# Custom timeout
npx copilot-pr-reviewer <PR_URL> --timeout 300
```

### Configuration File

Create `appsettings.json` in your project root:

```json
{
  "copilot": {
    "model": "gpt-4o",
    "maxTokensPerBatch": 50000,
    "overheadTokens": 5000,
    "maxParallelBatches": 2,
    "timeoutSeconds": 120
  },
  "review": {
    "postComments": true,
    "guidelinesPath": "./guidelines"
  },
  "azureDevOps": {
    "baseUrl": "https://dev.azure.com",
    "authType": "bearer"
  }
}
```

## CLI Options

| Option | Description | Default |
|--------|-------------|---------|
| `--pat <pat>` | Azure DevOps PAT | `$AZURE_DEVOPS_PAT` |
| `--auth-type <type>` | Auth type: 'pat', 'oauth' | `bearer` |
| `--model <model>` | AI model to use | `gpt-4o` |
| `--github-token <token>` | GitHub token for Copilot | `$GITHUB_TOKEN` |
| `--guidelines-path <path>` | Path to guidelines folder | Built-in guidelines |
| `--max-parallel <n>` | Max parallel batch reviews | `2` |
| `--no-comments` | Skip posting comments (dry run) | `false` |
| `--extend-review` | Review full code files in addition to diffs (default: diff-only mode) | `false` |
| `--timeout <seconds>` | Timeout in seconds | `120` |

## Custom Guidelines

Create custom review guidelines for your tech stack:

```bash
guidelines/
  ├── dotnet-guidelines.md
  ├── frontend-guidelines.md
  └── python-guidelines.md
```

Then reference them:

```bash
npx copilot-pr-reviewer <PR_URL> --guidelines-path ./guidelines
```

## CI/CD Integration

### GitHub Actions

```yaml
name: PR Review
on:
  pull_request:
    types: [opened, synchronize]

jobs:
  review:
    runs-on: ubuntu-latest
    steps:
      - name: Review PR with Copilot
        run: |
          npx copilot-pr-reviewer "${{ secrets.ADO_PR_URL }}"
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          AZURE_DEVOPS_PAT: ${{ secrets.AZURE_DEVOPS_PAT }}
```

### Azure Pipelines

```yaml
trigger:
  branches:
    include:
      - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: NodeTool@0
    inputs:
      versionSpec: '20.x'

  - script: |
      npx copilot-pr-reviewer "$(System.PullRequest.SourceRepositoryURI)/pullrequest/$(System.PullRequest.PullRequestId)"
    env:
      GITHUB_TOKEN: $(GITHUB_TOKEN)
      AZURE_DEVOPS_PAT: $(AZURE_DEVOPS_PAT)
    displayName: 'Run Copilot PR Review'
```

## Output Example

```
Reviewing PR #368461 in dnvgl-one/Engineering China/dapr-shop
PR: Add authentication feature (feature/auth → main)
Fetched 15 file changes
Created 3 batches, excluded 2 files
Reviewing batch 1 (Dotnet, 5 files, ~12500 tokens)
Batch 1 completed: 3 findings
Reviewing batch 2 (Frontend, 8 files, ~8200 tokens)
Batch 2 completed: 5 findings
Posting 8 comments to PR...

════════════════════════════════════════════════════════════
  Review Summary
════════════════════════════════════════════════════════════

  PR URL            https://dev.azure.com/...
  Title             Add authentication feature
  Total Files       15
  Reviewed Files    13
  Excluded Files    2
  Batches           3
  Total Findings    8
  Comments Posted   Yes
  Duration          45.2s

  Severity Breakdown:
    🔴 Critical: 2
    🟠 Major:    3
    🟡 Minor:    3
```

## Supported File Types

- **.NET**: `.cs`, `.csproj`, `.sln`, `.razor`, `.cshtml`
- **Frontend**: `.js`, `.jsx`, `.ts`, `.tsx`, `.vue`, `.html`, `.css`, `.scss`
- **Python**: `.py`, `.pyi`
- **Config**: `.json`, `.yaml`, `.yml`, `.http`, `.rest`

Automatically excludes:
- Lock files (`package-lock.json`, `yarn.lock`, etc.)
- Minified files (`.min.js`, `.min.css`)
- Generated files (`.d.ts`, `.g.cs`, `.Designer.cs`)

## Development

### Setup

```bash
git clone https://github.com/yourusername/copilot-pr-reviewer.git
cd copilot-pr-reviewer
npm install
```

### Build

```bash
npm run build
```

### Test

```bash
# Run all tests
npm test

# Run tests with UI
npm run test:ui

# Run with coverage
npm run test:coverage
```

### Local Testing

```bash
npm run dev -- <PR_URL>
```

## Publishing

This project uses GitHub Actions for automated npm publishing.

### Setup NPM Token

1. Create an npm token at [npmjs.com](https://www.npmjs.com/settings/~/tokens)
2. Add it as `NPM_TOKEN` secret in your GitHub repository settings

### Publish a Release

#### Option 1: GitHub Release (Recommended)

1. Create a new release on GitHub
2. Tag version (e.g., `v0.2.0`)
3. The workflow automatically publishes to npm

#### Option 2: Manual Workflow Dispatch

1. Go to Actions → Publish to NPM
2. Click "Run workflow"
3. Optionally specify version

#### Option 3: Manual Publish

```bash
# Update version
npm version patch  # or minor, major

# Publish
npm publish
```

## Architecture

```
┌─────────────────┐
│   CLI Entry     │
│   (index.ts)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Orchestrator   │ ──┐
│ (runReview)     │   │
└────────┬────────┘   │
         │            │
         ▼            ▼
┌─────────────────┐  ┌──────────────┐
│  ADO Client     │  │ Batch Builder│
│ (fetch PR data) │  │ (classify)   │
└─────────────────┘  └──────┬───────┘
                            │
                            ▼
                     ┌──────────────┐
                     │Review Service│
                     │(Copilot SDK) │
                     └──────┬───────┘
                            │
                            ▼
                     ┌──────────────┐
                     │   Reporter   │
                     │  (summary)   │
                     └──────────────┘
```

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing`)
5. Open a Pull Request

## License

MIT © Cuteribs

---

Made with ❤️ using GitHub Copilot SDK
