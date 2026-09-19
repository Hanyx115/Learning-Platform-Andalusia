# Set Up Andalusia Academy in Codex on Windows

This package is prepared for Windows 11 using the ChatGPT desktop app with Codex, PowerShell, Visual Studio or VS Code, .NET 10, Node.js, Git, SQL Server, and SSMS.

## 1. Extract the package

Extract this ZIP to a stable development folder such as:

```text
C:\Dev\AndalusiaAcademy
```

Avoid placing the repository inside a temporary Downloads folder. A normal Windows drive location is the simplest setup for the Windows-native Codex agent.

## 2. Install the ChatGPT desktop app

Open PowerShell and run:

```powershell
winget install --id 9PLM9XGG6VKS -s msstore
```

Open the app and sign in with the ChatGPT account that has Codex access.

## 3. Verify developer tools

Open PowerShell and run these commands individually:

```powershell
git --version
dotnet --version
node --version
npm --version
```

The project requires the .NET 10 SDK. Git and Node.js must also be available to the Windows-native Codex agent.

If Git is missing:

```powershell
winget install Git.Git
```

Restart ChatGPT, PowerShell, and your editor after installing developer tools so they reload the updated PATH.

## 4. Configure Codex

In the ChatGPT desktop app:

1. Open **Settings**.
2. Set the agent environment to **Windows native**.
3. Set the integrated terminal to **PowerShell**.
4. Set the preferred editor to **Visual Studio** or **VS Code**.
5. Keep the sandbox enabled and use **Ask for approval** rather than unrestricted full access.

Windows native is recommended initially because this project uses Visual Studio, SQL Server, and SSMS on Windows. WSL2 can be introduced later if there is a concrete need.

## 5. Open the project

In the ChatGPT desktop app:

1. Select **Codex**.
2. Click **Add new project**, or press `Ctrl+O`.
3. Choose:

```text
C:\Dev\AndalusiaAcademy
```

Codex automatically reads the root `AGENTS.md` before working. That file points Codex to the detailed prompt, scope, architecture, and current roadmap.

## 6. Initialize Git for a new repository

Skip this section if you already have a GitHub repository. In that case, clone the repository first and copy this package's files into the cloned repository without overwriting newer project work.

For a new repository, open the integrated PowerShell terminal in the project root:

```powershell
git init -b main
git status
git add AGENTS.md README.md SETUP_CODEX_WINDOWS.md FIRST_CODEX_PROMPT.txt .gitignore .gitattributes docs
git commit -m "docs(project): add architecture and Codex instructions"
git branch develop
git switch develop
git switch -c chore/solution-foundation
git status
```

Do not connect or push to GitHub until you have created the correct empty remote repository and verified that no secrets are staged.

## 7. Start the first Codex task

Open `FIRST_CODEX_PROMPT.txt`, copy its contents, and send it in the project chat.

Codex should:

- Confirm the instructions it loaded.
- Inspect Git status and the current branch.
- Read the relevant project documents.
- Implement Stage 1 only.
- Build the backend and frontend foundations.
- Stop with a Git checkpoint and colleague handoff.

## 8. Verify the Codex instruction setup

Ask Codex:

```text
Summarize the active project instructions, source priority, fixed stack, current stage, branch, and actions you must not take yet. Do not modify files.
```

The response should mention:

- .NET 10
- React 18 + TypeScript + Vite
- SQL Server
- Clean Architecture, CQRS, MediatR, and modular monolith
- Minimal safe changes
- Stage 1 only
- `chore/solution-foundation`
- No entities, migrations, Identity, Redis, Hangfire, or payment implementation yet

## 9. Recommended daily workflow

Before working:

```powershell
git status
git branch --show-current
git fetch origin
```

Do not pull, merge, or rebase a dirty working tree blindly.

At the end of a coherent step:

1. Review the Codex diff.
2. Run builds and relevant tests.
3. Stage only the intended files.
4. Commit with the exact recommended Conventional Commit message.
5. Push the feature branch.
6. Open a Pull Request into `develop` when the feature checkpoint is complete.

## 10. If PowerShell blocks npm

If PowerShell reports that `npm.ps1` cannot run because scripts are disabled, review your organization's policy before changing it. On a personal machine, a commonly used user-scoped setting is:

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

Do not disable Windows security or Application Control globally to fix a development command. Investigate the exact blocked executable or policy first.
