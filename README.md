# Cuteribs.CopilotPrReviewer

AI-powered pull request code reviewer for Azure DevOps using GitHub Copilot SDK.

## Features

- 🤖 **AI-Powered Reviews**: Uses GitHub Copilot to analyze code changes
- 📊 **Tech Stack Detection**: Automatically applies appropriate guidelines for .NET, Frontend (JS/TS/React), and Python
- 🎯 **Smart Batching**: Intelligently groups files by tech stack and token limits for efficient parallel processing
- 🚫 **Smart Filtering**: Excludes lock files, generated files, and minified code
- 💬 **Direct PR Comments**: Posts findings directly to Azure DevOps pull requests
- 📈 **Rich Console Output**: Beautiful summary reports with Spectre.Console

## Prerequisites

- .NET 10 SDK
- GitHub Copilot CLI installed and authenticated
- Azure DevOps Personal Access Token (PAT) with Code (Read & Write) permissions

## Installation

### Option 1: Global Installation (Recommended)

Install as a global .NET tool for use anywhere:

```bash
# PowerShell / bash
dotnet tool install -g Cuteribs.CopilotPrReviewer
```

Once installed, run from anywhere:
```bash
copilot-pr-reviewer <pr-url> [options]
```

### Option 2: Direct Execution with `dnx` (No Installation)

Run directly without installation (similar to `npx`):

```bash
# PowerShell / bash
dnx Cuteribs.CopilotPrReviewer <pr-url> [options]
```

This is useful for:
- CI/CD pipelines (no tool installation needed)
- Temporary usage without cluttering global tools
- Always running the latest version

## Configuration

### Required Environment Variables

#### 1. Azure DevOps Authentication

Set your Azure DevOps Personal Access Token:

**PowerShell:**
```powershell
$env:AzureDevOps__Pat = "your-ado-pat-token"
```

**Command Prompt (cmd.exe):**
```batch
set AzureDevOps__Pat=your-ado-pat-token
```

**Bash (Linux/macOS):**
```bash
export AzureDevOps__Pat="your-ado-pat-token"
```

**Creating an Azure DevOps PAT:**
1. Navigate to https://dev.azure.com/{your-organization}/_usersSettings/tokens
2. Click "New Token"
3. Name: `Cuteribs.CopilotPrReviewer`
4. Select **Code (Read & Write)** scope
5. Expiration: Set as needed (90 days recommended)
6. Click "Create"
7. **Copy the token immediately** (won't be shown again)

#### 2. GitHub Copilot Authentication

The GitHub Copilot SDK requires authentication via GitHub CLI or `GITHUB_TOKEN` environment variable.

**Option A: GitHub CLI Authentication (Recommended)**

Install GitHub CLI:

**Windows (PowerShell with Chocolatey):**
```powershell
choco install gh
```

**Windows (with WinGet):**
```powershell
winget install GitHub.cli
```

**macOS:**
```bash
brew install gh
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt install gh
```

Then authenticate:
```bash
gh auth login
```

Follow the prompts:
1. Select `GitHub.com`
2. Select `HTTPS`
3. Authenticate with your browser
4. Authorize GitHub CLI

**Option B: Personal Access Token**

You can also authenticate using a fine-grained PAT with the "Copilot Requests" permission enabled.

1. Visit https://github.com/settings/personal-access-tokens/new
2. Under "Permissions," click "add permissions" and select "Copilot Requests"
3. Generate your token
7. **Copy the token immediately** (won't be shown again)

Set the environment variable:

**PowerShell:**
```powershell
$env:GITHUB_TOKEN = "ghp_your-github-token"
```

**Command Prompt (cmd.exe):**
```batch
set GITHUB_TOKEN=ghp_your-github-token
```

**Bash (Linux/macOS):**
```bash
export GITHUB_TOKEN="ghp_your-github-token"
```

**Note**: For more details, see:
- [GitHub CLI Documentation](https://cli.github.com/manual/)
- [GitHub Copilot CLI README](https://github.com/github/copilot-cli/blob/main/README.md)

### Optional Configuration

#### appsettings.json

You can create an `appsettings.json` file in the execution directory:

```json
{
  "AzureDevOps": {
    "BaseUrl": "https://dev.azure.com"
  },
  "Copilot": {
    "Model": "gpt-5-mini",
    "MaxTokensPerBatch": 90000,
    "OverheadTokens": 20000,
    "MaxParallelBatches": 4
  },
  "Review": {
    "GuidelinesPath": "",
    "PostComments": true
  }
}
```

#### Environment Variables

All settings can be overridden with environment variables using the prefix `COPILOT_PR_REVIEWER_`:

**PowerShell:**
```powershell
$env:COPILOT_PR_REVIEWER_Copilot__Model = "gpt-5"
$env:COPILOT_PR_REVIEWER_Copilot__MaxParallelBatches = "8"
```

**Command Prompt (cmd.exe):**
```batch
set COPILOT_PR_REVIEWER_Copilot__Model=gpt-5
set COPILOT_PR_REVIEWER_Copilot__MaxParallelBatches=8
```

**Bash (Linux/macOS):**
```bash
export COPILOT_PR_REVIEWER_Copilot__Model="gpt-5"
export COPILOT_PR_REVIEWER_Copilot__MaxParallelBatches="8"
```

## Usage

### Basic Usage

Review a pull request:

```bash
copilot-pr-reviewer <Pull Request URL>
```

Or with `dnx` (no installation required):

```bash
dnx Cuteribs.CopilotPrReviewer <Pull Request URL>
```

For CI/CD (e.g., Azure Pipelines YAML):

```yaml
- script: |
    copilot-pr-reviewer $(PR_URL)
  displayName: 'Review PR with AI'
  env:
    GITHUB_TOKEN: $(GITHUB_TOKEN)
    AzureDevOps__Pat: $(AZURE_DEVOPS_PAT)
```

Or using `dnx` (preferred for CI/CD):

```yaml
- script: dnx Cuteribs.CopilotPrReviewer $(PR_URL)
  displayName: 'Review PR with AI'
  env:
    AzureDevOps__Pat: $(AZURE_DEVOPS_PAT)
    GITHUB_TOKEN: $(GITHUB_TOKEN)
```

```bash
# Specify PAT via CLI
copilot-pr-reviewer <Pull Request URL> \
  --pat "your-ado-pat"

# Use a different AI model
copilot-pr-reviewer <Pull Request URL> \
  --model "gpt-5"

# Dry run (no comments posted)
copilot-pr-reviewer <Pull Request URL> \
  --no-comments

# Use custom guidelines
copilot-pr-reviewer <Pull Request URL> \
  --guidelines-path "./custom-guidelines"

# Maximum parallel batches
copilot-pr-reviewer <Pull Request URL> \
  --max-parallel 8
```

### Full Command Options

```
Options:
  --pr-url <pr-url> (REQUIRED)         Azure DevOps pull request URL
  --pat <pat>                          Azure DevOps personal access token
  --model <model>                      AI model to use for review (default: gpt-5-mini)
  --guidelines-path <guidelines-path>  Path to external guidelines folder
  --max-parallel <max-parallel>        Maximum parallel batch reviews (default: 4)
  --no-comments                        Skip posting comments to PR (dry run)
  --version                            Show version information
  -?, -h, --help                       Show help and usage information
```

## How It Works

Both Azure DevOps URL formats are supported:

- `https://dev.azure.com/{org}/{project}/_git/{repo}/pullrequest/{id}`
- `https://{org}.visualstudio.com/{project}/_git/{repo}/pullrequest/{id}`

## What is `dnx`?

`dnx` is the .NET equivalent of `npx` (Node.js). It allows you to run .NET global tools directly without installing them globally.

**Benefits:**
- No installation needed
- Always runs the latest version
- Perfect for CI/CD pipelines
- Avoids global tool clutter

**Requirements:** .NET 10 SDK or later

**Usage:**
```bash
dnx Cuteribs.CopilotPrReviewer <pr-url> [options]
```

**Example:**
```bash
# Run directly without installation
dnx Cuteribs.CopilotPrReviewer <Pull Request URL> --no-comments

# In CI/CD (Azure Pipelines)
- script: dnx Cuteribs.CopilotPrReviewer $(PR_URL)
  env:
    AzureDevOps__Pat: $(AZURE_DEVOPS_PAT)
    GITHUB_TOKEN: $(GITHUB_TOKEN)
```

**Recommended for:**
- CI/CD pipelines (no tool installation step needed)
- Temporary usage
- Shared team environments
- Quick testing

## How It Works

1. **Fetch PR Changes**: Downloads all changed files and their diffs from Azure DevOps
2. **Classify Files**: Categorizes files by tech stack (.NET, Frontend, Python, Config)
3. **Filter Exclusions**: Removes lock files, generated files, and minified code
4. **Create Batches**: Groups files by tech stack within token limits for optimal processing
5. **Parallel Review**: Reviews batches in parallel using GitHub Copilot with appropriate guidelines
6. **Post Comments**: Adds findings as threaded comments on the PR at specific file/line locations
7. **Summary Report**: Displays a rich console summary with severity breakdown

## Review Guidelines

The tool includes embedded guidelines for:

- **.NET**: Comprehensive C# best practices (security, performance, architecture, Clean Architecture, DDD)
- **Frontend**: JavaScript/TypeScript/React best practices (security, performance, accessibility)
- **Python**: Python best practices (security, code quality, performance)

### Custom Guidelines

You can override the built-in guidelines by providing a custom guidelines folder:

```bash
copilot-pr-reviewer https://dev.azure.com/... --guidelines-path "./my-guidelines"
```

Place your custom guideline files in the folder:
- `dotnet-guidelines.md`
- `frontend-guidelines.md`
- `python-guidelines.md`

## Finding Severity Levels

- **Critical**: Security vulnerabilities, data loss risks, production crashes
- **High**: Performance issues, correctness problems, significant best practice violations
- **Medium**: Code quality concerns, maintainability issues
- **Low**: Style issues, minor optimizations, documentation gaps

## CI/CD Integration

### Azure Pipelines

```yaml
- task: UseDotNet@2
  displayName: 'Install .NET 10 SDK'
  inputs:
    packageType: 'sdk'
    version: '10.x'

- script: |
    dnx Cuteribs.CopilotPrReviewer $(System.PullRequest.SourceRepositoryUri)/pullrequest/$(System.PullRequest.PullRequestId)
  displayName: 'Review PR with AI'
  env:
    AzureDevOps__Pat: $(AZURE_DEVOPS_PAT)
    GITHUB_TOKEN: $(GITHUB_TOKEN)
```

### GitHub Actions (for Azure DevOps PRs)

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '10.0.x'

- name: Review PR
  run: dnx Cuteribs.CopilotPrReviewer ${{ github.event.pull_request.html_url }}
  env:
    AzureDevOps__Pat: ${{ secrets.AZURE_DEVOPS_PAT }}
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

## Troubleshooting

### "Azure DevOps PAT is not configured"

Set the `AzureDevOps__Pat` environment variable or provide it via `--pat` argument.

### "GitHub Copilot CLI not found"

Install and authenticate with GitHub CLI:
```bash
# Install GitHub CLI
winget install GitHub.Copilot       # Windows
brew install copilot-cli            # macOS
npm install -g @github/copilot
```

Or set the `GITHUB_TOKEN` environment variable.

### "The PR is not active" or "The PR has merge conflict"

The tool only reviews active PRs without merge conflicts. Resolve any conflicts first.

### "No supported code file found in this PR"

The PR only contains files that aren't in supported tech stacks (.NET, Frontend, Python). Config files (JSON, YAML) are excluded from review.

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## Acknowledgments

- Built with [GitHub Copilot SDK](https://www.nuget.org/packages/GitHub.Copilot.SDK)
- Diff generation by [DiffPlex](https://github.com/mmanela/diffplex)
- Console UI by [Spectre.Console](https://spectreconsole.net/)
- Token counting by [SharpToken](https://github.com/dmitry-brazhenko/SharpToken)
