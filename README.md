# CopilotPrReviewer

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
- Azure DevOps authentication via OAuth, or Personal Access Token (PAT) with Code (Read & Write) permissions

## Installation

### Option 1: Global Installation (Recommended)

Install as a global .NET tool for use anywhere:

```bash
# PowerShell / bash
dotnet tool install -g CopilotPrReviewer
```

Once installed, run from anywhere:
```bash
CopilotPrReviewer <pr-url> [options]
```

### Option 2: Direct Execution with `dnx` (No Installation)

Run directly without installation (similar to `npx`):

```bash
# PowerShell / bash
dnx CopilotPrReviewer <pr-url> [options]
```

This is useful for:
- CI/CD pipelines (no tool installation needed)
- Temporary usage without cluttering global tools
- Always running the latest version

## Configuration

### Required: Azure DevOps Authentication

You have two authentication options for Azure DevOps:

#### Option 1: OAuth (Recommended - Interactive Browser Login)

No setup required! The app will automatically open your browser for authentication on first use.

```bash
CopilotPrReviewer <Pull Request URL>
```

#### Option 2: Personal Access Token (PAT)

Provide your PAT via CLI argument or environment variable:

**Via CLI Argument:**
```bash
CopilotPrReviewer <Pull Request URL> --pat "your-ado-pat-token"
```

**Via Environment Variable:**

**PowerShell:**
```powershell
$env:AZURE_DEVOPS_PAT = "your-ado-pat-token"
CopilotPrReviewer <Pull Request URL>
```

**Command Prompt (cmd.exe):**
```batch
set AZURE_DEVOPS_PAT=your-ado-pat-token
CopilotPrReviewer <Pull Request URL>
```

**Bash (Linux/macOS):**
```bash
export AZURE_DEVOPS_PAT="your-ado-pat-token"
CopilotPrReviewer <Pull Request URL>
```

**Creating an Azure DevOps PAT:**
1. Navigate to https://dev.azure.com/{your-organization}/_usersSettings/tokens
2. Click "New Token"
4. Select **Code (Read & Write)** scope
5. Expiration: Set as needed (90 days recommended)
6. Click "Create"
7. **Copy the token immediately** (won't be shown again)

### Required: GitHub Copilot Authentication

The GitHub Copilot SDK requires authentication via GitHub CLI or `GITHUB_TOKEN` environment variable.

**Option A: GitHub CLI Authentication (Recommended)**

Install GitHub CLI (has to be installed via npm):

```bash
npm install -g @github/copilot
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

You can create an `appsettings.json` file in the execution directory to customize Copilot and Review settings:

```json
{
  "AzureDevOps": {
    "BaseUrl": "https://dev.azure.com"
  },
  "Copilot": {
    "Model": "gpt-5-mini",
    "MaxTokensPerBatch": 90000,
    "OverheadTokens": 20000,
    "MaxParallelBatches": 4,
    "TimeoutSeconds": 300
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

Review a pull request with OAuth (interactive browser login):

```bash
CopilotPrReviewer <Pull Request URL>
```

Or with `dnx` (no installation required):

```bash
dnx CopilotPrReviewer <Pull Request URL>
```

For CI/CD (e.g., Azure Pipelines YAML with OAuth):

```yaml
- script: |
    dnx CopilotPrReviewer $(System.PullRequest.SourceRepositoryUri)/pullrequest/$(System.PullRequest.PullRequestId)
  displayName: 'Review PR with AI (OAuth)'
  env:
    GITHUB_TOKEN: $(GITHUB_TOKEN)
```

Or using PAT (if interactive login is not possible):

```yaml
- script: dnx CopilotPrReviewer $(System.PullRequest.SourceRepositoryUri)/pullrequest/$(System.PullRequest.PullRequestId)
  displayName: 'Review PR with AI (PAT)'
  env:
    GITHUB_TOKEN: $(GITHUB_TOKEN)
    AZURE_DEVOPS_PAT: $(AZURE_DEVOPS_PAT)
```

### Advanced Usage Examples

```bash
# OAuth (default, interactive browser login - no extra config needed!)
CopilotPrReviewer <Pull Request URL>

# PAT via CLI argument
CopilotPrReviewer <Pull Request URL> --pat "your-ado-pat"

# PAT via environment variable (from AZURE_DEVOPS_PAT)
CopilotPrReviewer <Pull Request URL>

# Explicit authentication type
CopilotPrReviewer <Pull Request URL> --auth-type oauth
CopilotPrReviewer <Pull Request URL> --auth-type pat

# Use a different AI model
CopilotPrReviewer <Pull Request URL> --model "gpt-5"

# Dry run (no comments posted)
CopilotPrReviewer <Pull Request URL> --no-comments

# Use custom guidelines
CopilotPrReviewer <Pull Request URL> --guidelines-path "./custom-guidelines"

# Maximum parallel batches
CopilotPrReviewer <Pull Request URL> --max-parallel 8

# Custom timeout
CopilotPrReviewer <Pull Request URL> --timeout 120

# GitHub token for Copilot authentication
CopilotPrReviewer <Pull Request URL> --github-token "github_pat_your-github-token"
```

### Full Command Options

```
Options:
  --pr-url <pr-url> (REQUIRED)         Azure DevOps pull request URL
  --pat <pat>                          Azure DevOps personal access token (optional)
  --auth-type <auth-type>              Authentication type: 'pat', 'oauth' (default: auto-detect)
  --model <model>                      AI model to use for review (default: gpt-5-mini)
  --github-token <github-token>        GitHub token for Copilot authentication (optional)
  --guidelines-path <guidelines-path>  Path to external guidelines folder
  --max-parallel <max-parallel>        Maximum parallel batch reviews (default: 4)
  --no-comments                        Skip posting comments to PR (dry run)
  --timeout <timeout>                  Timeout for the review process (in seconds, default: 300)
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
dnx CopilotPrReviewer <pr-url> [options]
```

**Example:**
```bash
# Run directly without installation
dnx CopilotPrReviewer <Pull Request URL> --no-comments

# In CI/CD (Azure Pipelines)
- script: dnx CopilotPrReviewer $(PR_URL)
  env:
    AZURE_DEVOPS_PAT: $(AZURE_DEVOPS_PAT)
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
CopilotPrReviewer https://dev.azure.com/... --guidelines-path "./my-guidelines"
```

Place your custom guideline files in the folder:
- `dotnet-guidelines.md`
- `frontend-guidelines.md`
- `python-guidelines.md`

## Finding Severity Levels

| Severity | Description | Action Required |
|----------|-------------|-----------------|
| **🔴 Critical** | Security vulnerabilities, data loss risks, production crashes, blocking bugs | Must fix before merge |
| **🟠 Major** | Performance issues, code correctness problems, maintainability concerns, significant best practice violations | Should fix before merge |
| **🟡 Minor** | Code style issues, minor optimizations, documentation gaps, suggestions for improvement | Can fix in follow-up PR |

## CI/CD Integration

### Azure Pipelines (Using OAuth - No PAT Needed!)

The recommended approach for Azure Pipelines is to use OAuth since the tool automatically handles interactive authentication in supported environments:

```yaml
- task: UseDotNet@2
  displayName: 'Install .NET 10 SDK'
  inputs:
    packageType: 'sdk'
    version: '10.x'

- script: |
    dnx CopilotPrReviewer $(System.PullRequest.SourceRepositoryUri)/pullrequest/$(System.PullRequest.PullRequestId)
  displayName: 'Review PR with AI (OAuth)'
  env:
    GITHUB_TOKEN: $(GITHUB_TOKEN)
```

### Azure Pipelines (Using PAT - For Non-Interactive Environments)

If running in a non-interactive environment without browser access, use PAT:

```yaml
- task: UseDotNet@2
  displayName: 'Install .NET 10 SDK'
  inputs:
    packageType: 'sdk'
    version: '10.x'

- script: |
    dnx CopilotPrReviewer $(System.PullRequest.SourceRepositoryUri)/pullrequest/$(System.PullRequest.PullRequestId)
  displayName: 'Review PR with AI (PAT)'
  env:
    GITHUB_TOKEN: $(GITHUB_TOKEN)
    AZURE_DEVOPS_PAT: $(AZURE_DEVOPS_PAT)
```

### GitHub Actions (for Azure DevOps PRs)

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '10.0.x'

- name: Review PR
  run: dnx CopilotPrReviewer ${{ github.event.pull_request.html_url }}
  env:
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    AZURE_DEVOPS_PAT: ${{ secrets.AZURE_DEVOPS_PAT }}
```

## Troubleshooting

### "Azure DevOps PAT is not configured"

This error means the tool is set to use PAT authentication but no token was found. Provide it via:

1. **CLI argument:** `--pat "your-ado-pat"`
2. **Environment variable:** `AZURE_DEVOPS_PAT=your-ado-pat`

Or switch to OAuth (default):
```bash
CopilotPrReviewer <Pull Request URL>
```

### Browser doesn't open for OAuth login

If you're in a non-interactive or headless environment (CI/CD), you can:
1. Use PAT authentication instead (set `AZURE_DEVOPS_PAT` environment variable)
2. Or explicitly specify: `--auth-type pat`

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
