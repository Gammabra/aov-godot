# CONTRIBUTING.md

**Language / Langue:** [🇬🇧 English](#-contributing-guide) | [🇫🇷 Français](#-guide-de-contribution)

## 🇬🇧 Contributing Guide

Thank you for considering contributing to **Ashes of Velsingrad**!

We are a small indie team, and we want to keep the code clean, maintainable, and consistent with our design vision. Please read these guidelines carefully before you start working.

### 💡 Branching model

We use **three main branches**:

- `develop`: main working branch. All features and fixes start here.
- `canary`: beta branch. We merge `develop` into `canary` when we want to test features in a pre-release context.
- `release`: final release-ready branch. We only merge into `release` when `canary` is fully tested and stable.

**Never push directly to `canary` or `release`!**

### 🚀 How to contribute

# 1️⃣ **Fork and clone the repo**

```bash
git clone <your fork url>
cd ashes-of-velsingrad
````

# 2️⃣ **Create a feature branch from `develop`**

```bash
git checkout develop
git checkout -b feat/your-feature-name
```

# 3️⃣ **Follow code standards**

* Use English for code comments and identifiers.
* Keep commits small and meaningful (avoid huge "dump everything" commits).
* Include tests or test cases whenever possible.
* Document new functions or modules clearly.

# 4️⃣ **Commit message convention**

We follow the [Conventional Commits](https://www.conventionalcommits.org/) convention to maintain a clear and consistent history.

## Mandatory format

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

## Allowed types

| Type | Description | Example |
|------|-------------|---------|
| `feat` | New feature | `feat(combat): add critical hit system` |
| `fix` | Bug fix | `fix(inventory): prevent null item crash` |
| `docs` | Documentation only | `docs(readme): update build instructions` |
| `style` | Formatting, missing semicolons, etc. | `style(player): fix code formatting` |
| `refactor` | Code refactoring without functionality change | `refactor(combat): simplify damage calculation` |
| `test` | Adding or modifying tests | `test(inventory): add unit tests for sorting` |
| `chore` | Maintenance, config, tools | `chore(ci): update GitHub Actions workflow` |
| `perf` | Performance improvements | `perf(rendering): optimize shadow calculations` |

## Available scopes

| Scope | Description | Examples |
|-------|-------------|----------|
| `player` | Player system (movement, stats, etc.) | `feat(player): add dodge mechanic` |
| `combat` | Combat system | `fix(combat): balance damage formula` |
| `inventory` | Inventory/item management | `feat(inventory): add item sorting` |
| `ui` | User interface | `fix(ui): menu button alignment` |
| `audio` | Sound and music | `feat(audio): add ambient sounds` |
| `level` | Level design, environment | `feat(level): add destructible objects` |
| `ai` | Artificial intelligence/enemies | `fix(ai): improve pathfinding` |
| `save` | Save system | `fix(save): prevent data corruption` |
| `network` | Multiplayer (if applicable) | `feat(network): add lobby system` |
| `build` | Build/deployment system | `chore(build): optimize export settings` |
| `config` | Project configuration | `chore(config): update project settings` |

## Description rules

- **Length**: Maximum 50 characters
- **Language**: English only
- **Style**: Imperative present ("add" not "adds" or "added")
- **Case**: First letter lowercase
- **Punctuation**: No ending period

## Valid examples

```bash
feat(player): add wall jump ability
fix(combat): resolve enemy freeze on death
docs(api): document new weapon system
refactor(inventory): extract item validation logic
test(save): add integration tests for game state
chore(deps): update Godot to 4.2.1
perf(rendering): cache frequently used shaders
```

## Automatic validation

To avoid errors, we recommend using **VSCode extensions** and **Git hooks**:

### Method 1: VSCode Extensions (Recommended)

Install these VSCode extensions:
- **Conventional Commits** (by vivaxy) - Strongly recommended
- **Git Commit Plugin** (by redjue) - Optional, adds commit templates
- **GitLens** (by GitKraken) - Optional, better Git integration

Configure the **Conventional Commits** extension in VSCode settings:

```json
// .vscode/settings.json
{
  "conventionalCommits.scopes": [
    "player",
    "combat",
    "inventory",
    "ui",
    "audio",
    "level",
    "ai",
    "save",
    "network",
    "build",
    "config"
  ],
  "conventionalCommits.showEditor": true,
  "conventionalCommits.promptBody": true,
  "conventionalCommits.promptFooter": false
}
```

### Method 2: Git Hooks

Create a simple commit validation script:

```bash
# .git/hooks/commit-msg (make it executable: chmod +x .git/hooks/commit-msg)
#!/bin/bash

commit_message=$(cat "$1")

# Check if commit message follows conventional format
if ! echo "$commit_message" | grep -qE "^(feat|fix|docs|style|refactor|test|chore|perf)(\(.+\))?: .+"; then
    echo "❌ Invalid commit message format!"
    echo "Format: <type>(<scope>): <description>"
    echo "Example: feat(player): add wall jump ability"
    exit 1
fi

# Check if scope is valid (optional)
scope=$(echo "$commit_message" | grep -oE "\(.+\)" | sed 's/[()]//g')
if [ -n "$scope" ]; then
    valid_scopes="player|combat|inventory|ui|audio|level|ai|save|network|build|config"
    if ! echo "$scope" | grep -qE "^($valid_scopes)$"; then
        echo "⚠️  Warning: '$scope' is not a standard scope"
        echo "Valid scopes: player, combat, inventory, ui, audio, level, ai, save, network, build, config"
    fi
fi

echo "✅ Commit message format is valid"
```

### Method 3: Global Git Template

Create a commit template to guide team members:

```bash
# .gitmessage
# <type>(<scope>): <subject>
#
# <body>
#
# <footer>

# Types: feat, fix, docs, style, refactor, test, chore, perf
# Scopes: player, combat, inventory, ui, audio, level, ai, save, network, build, config
# Subject: imperative, present tense, lowercase, no period
# Body: explain what and why (optional)
# Footer: breaking changes, issues closed (optional)
```

Then configure Git to use it:
```bash
git config commit.template .gitmessage
```

## In case of error

If your commit is rejected:

1. Check the type and scope used
2. Follow the exact format: `type(scope): description`
3. Avoid custom types like "feature", "add", "update"

## Special commits

### Breaking changes
```bash
feat(combat)!: redesign damage system

BREAKING CHANGE: old damage calculations are no longer compatible
```

### Multiple scopes
```bash
feat(player,combat): add stamina system affecting attacks
```

### Without scope (discouraged)
```bash
chore: update .gitignore
```

# 5️⃣ **Submit a pull request (PR)**

* Target **`develop`**.
* Add a clear PR description: what, why, screenshots or logs if relevant.
* Reference issues (e.g., `Fixes #42`).

# 6️⃣ **Code review**

* Another team member reviews your PR.
* Address feedback before merging.
* Once approved, it will be merged into `develop`.

### 🧹 Other practices

* Use conventional commit messages (see above).
* Check your code with linters and static analysis tools (we provide configs).
* Update relevant documentation if needed.

## 💬 Questions?

If something is unclear, ask in the team Discord or open a discussion in the repository.

---

## 🇫🇷 Guide de contribution

Merci d’envisager de contribuer à **Ashes of Velsingrad** !

Nous sommes une petite équipe indépendante, et nous tenons à garder un code propre, maintenable et cohérent avec notre vision artistique. Merci de lire ces instructions attentivement avant de commencer.

### 💡 Modèle de branches

Nous utilisons **trois branches principales** :

* `develop` : branche principale de travail. Toutes les features et correctifs commencent ici.
* `canary` : branche bêta. On merge `develop` dans `canary` quand on veut tester les fonctionnalités en contexte pré-release.
* `release` : branche finale prête à publier. On merge seulement `canary` dans `release` quand tout est stable et validé.

**Ne jamais pousser directement sur `canary` ou `release` !**

### 🚀 Comment contribuer

# 1️⃣ **Forker et cloner le dépôt**

```bash
git clone <url de votre fork>
cd ashes-of-velsingrad
```

# 2️⃣ **Créer une branche feature à partir de `develop`**

```bash
git checkout develop
git checkout -b feat/nom-de-votre-feature
```

# 3️⃣ **Respecter les standards**

* Utiliser l’anglais pour les commentaires et identifiants.
* Commits petits et explicites (éviter les gros commits "dump").
* Ajouter des tests si possible.
* Bien documenter les nouvelles fonctions ou modules.

# 4️⃣ **Commit message convention**

Nous suivons la convention [Conventional Commits](https://www.conventionalcommits.org/) pour maintenir un historique clair et cohérent.

## Format obligatoire

```
<type>(<scope>): <description>

[body optionnel]

[footer optionnel]
```

## Types autorisés

| Type | Description | Exemple |
|------|-------------|---------|
| `feat` | Nouvelle fonctionnalité | `feat(combat): add critical hit system` |
| `fix` | Correction de bug | `fix(inventory): prevent null item crash` |
| `docs` | Documentation uniquement | `docs(readme): update build instructions` |
| `style` | Formatage, point-virgules manquants, etc. | `style(player): fix code formatting` |
| `refactor` | Refactoring sans changement de fonctionnalité | `refactor(combat): simplify damage calculation` |
| `test` | Ajout ou modification de tests | `test(inventory): add unit tests for sorting` |
| `chore` | Maintenance, config, outils | `chore(ci): update GitHub Actions workflow` |
| `perf` | Amélioration des performances | `perf(rendering): optimize shadow calculations` |

## Scopes disponibles

| Scope | Description | Exemples |
|-------|-------------|----------|
| `player` | Système du joueur (mouvement, stats, etc.) | `feat(player): add dodge mechanic` |
| `combat` | Système de combat | `fix(combat): balance damage formula` |
| `inventory` | Gestion inventaire/objets | `feat(inventory): add item sorting` |
| `ui` | Interface utilisateur | `fix(ui): menu button alignment` |
| `audio` | Sons et musique | `feat(audio): add ambient sounds` |
| `level` | Design de niveau, environnement | `feat(level): add destructible objects` |
| `ai` | Intelligence artificielle/ennemis | `fix(ai): improve pathfinding` |
| `save` | Système de sauvegarde | `fix(save): prevent data corruption` |
| `network` | Multijoueur (si applicable) | `feat(network): add lobby system` |
| `build` | Système de build/déploiement | `chore(build): optimize export settings` |
| `config` | Configuration projet | `chore(config): update project settings` |

## Règles de description

- **Longueur** : Maximum 50 caractères
- **Langue** : Anglais uniquement
- **Style** : Impératif présent ("add" pas "adds" ou "added")
- **Majuscule** : Première lettre en minuscule
- **Ponctuation** : Pas de point final

## Exemples valides

```bash
feat(player): add wall jump ability
fix(combat): resolve enemy freeze on death
docs(api): document new weapon system
refactor(inventory): extract item validation logic
test(save): add integration tests for game state
chore(deps): update Godot to 4.2.1
perf(rendering): cache frequently used shaders
```

## Validation automatique

Pour éviter les erreurs, nous recommandons d'utiliser les **extensions VSCode** et les **Git hooks** :

### Méthode 1 : Extensions VSCode (Recommandée)

Installez ces extensions VSCode :
- **Conventional Commits** (par vivaxy) - Recommandée fortement
- **Git Commit Plugin** (par redjue) - Optionnel, ajoute des templates de commit
- **GitLens** (par GitKraken) - Optionnel, meilleure intégration Git

Configurez l'extension **Conventional Commits** dans les paramètres VSCode :

```json
// .vscode/settings.json
{
  "conventionalCommits.scopes": [
    "player",
    "combat",
    "inventory",
    "ui",
    "audio",
    "level",
    "ai",
    "save",
    "network",
    "build",
    "config"
  ],
  "conventionalCommits.showEditor": true,
  "conventionalCommits.promptBody": true,
  "conventionalCommits.promptFooter": false
}
```

### Méthode 2 : Git Hooks

Créez un script de validation simple :

```bash
# .git/hooks/commit-msg (le rendre exécutable : chmod +x .git/hooks/commit-msg)
#!/bin/bash

commit_message=$(cat "$1")

# Vérifier si le message suit le format conventionnel
if ! echo "$commit_message" | grep -qE "^(feat|fix|docs|style|refactor|test|chore|perf)(\(.+\))?: .+"; then
    echo "❌ Format de message de commit invalide !"
    echo "Format : <type>(<scope>): <description>"
    echo "Exemple : feat(player): add wall jump ability"
    exit 1
fi

# Vérifier si le scope est valide (optionnel)
scope=$(echo "$commit_message" | grep -oE "\(.+\)" | sed 's/[()]//g')
if [ -n "$scope" ]; then
    valid_scopes="player|combat|inventory|ui|audio|level|ai|save|network|build|config"
    if ! echo "$scope" | grep -qE "^($valid_scopes)$"; then
        echo "⚠️  Attention : '$scope' n'est pas un scope standard"
        echo "Scopes valides : player, combat, inventory, ui, audio, level, ai, save, network, build, config"
    fi
fi

echo "✅ Format du message de commit valide"
```

### Méthode 3 : Template Git global

Créez un template de commit pour guider les membres de l'équipe :

```bash
# .gitmessage
# <type>(<scope>): <subject>
#
# <body>
#
# <footer>

# Types : feat, fix, docs, style, refactor, test, chore, perf
# Scopes : player, combat, inventory, ui, audio, level, ai, save, network, build, config
# Subject : impératif, présent, minuscule, pas de point
# Body : expliquer quoi et pourquoi (optionnel)
# Footer : breaking changes, issues fermées (optionnel)
```

Puis configurez Git pour l'utiliser :
```bash
git config commit.template .gitmessage
```

## En cas d'erreur

Si votre commit est rejeté :

1. Vérifiez le type et le scope utilisés
2. Respectez le format exact : `type(scope): description`
3. Évitez les types personnalisés comme "feature", "add", "update"

## Commits spéciaux

### Breaking changes
```bash
feat(combat)!: redesign damage system

BREAKING CHANGE: old damage calculations are no longer compatible
```

### Multiples scopes
```bash
feat(player,combat): add stamina system affecting attacks
```

### Sans scope (découragé)
```bash
chore: update .gitignore
```

# 5️⃣ **Soumettre un pull request (PR)**

* Cibler **`develop`**.
* Ajouter une description claire : quoi, pourquoi, captures d’écran ou logs si nécessaire.
* Référencer les issues (ex: `Fixes #42`).

# 6️⃣ **Review**

* Un membre de l’équipe relit votre PR.
* Apporter les corrections demandées avant merge.
* Une fois approuvée, la PR est mergée dans `develop`.

### 🧹 Autres bonnes pratiques

* Utiliser des messages de commit conventionnels (voir ci-dessus).
* Vérifier le code avec les linters et outils d’analyse statique (configs fournies).
* Mettre à jour la documentation concernée.

## 💬 Des questions?

Si quelque chose n'est pas clair, merci de demander sur le Discord de l'équipe ou d'ouvrir une discussion dans le dépôt.