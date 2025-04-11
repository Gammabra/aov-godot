---
name: 🛠️ Script or Tooling Task
about: Create or maintain dev tooling (Node.js, automation scripts, etc.)
title: "[Tooling] ..."
labels: ['🛠️ scripts', 'ci']
assignees: ''
---

## ⚙️ Tooling Goal

> _What does the script or tooling solve?_  
> _E.g., “Auto-bump version on tag”, “Generate markdown docs from C# summaries”_

## 📂 Script Path

> _Where will this script live?_  
> `tools/git-manager.js` or similar.

## 📥 Input & 📤 Output

> _What does the script take in, and what does it produce?_  
> E.g., parses Git log, outputs Markdown changelog.

## 🔁 Usage Flow

```bash
# Example command to run
node tools/my-script.js --type lint
```

## 🧪 Test Plan

> _How will we verify this works? Should it be included in CI?_

## 📎 Related Issues / Workflows

> _Mention anything this connects to (PR automation, CI, etc.)_
