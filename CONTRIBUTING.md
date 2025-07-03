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

1️⃣ **Fork and clone the repo**

```bash
git clone <your fork url>
cd ashes-of-velsingrad
````

2️⃣ **Create a feature branch from `develop`**

```bash
git checkout develop
git checkout -b feat/your-feature-name
```

3️⃣ **Follow code standards**

- Use English for code comments and identifiers.
- Keep commits small and meaningful (avoid huge "dump everything" commits).
- Include tests or test cases whenever possible.
- Document new functions or modules clearly.

4️⃣ **Submit a pull request (PR)**

- Target **`develop`**.
- Add a clear PR description: what, why, screenshots or logs if relevant.
- Reference issues (e.g., `Fixes #42`).

5️⃣ **Code review**

- Another team member reviews your PR.
- Address feedback before merging.
- Once approved, it will be merged into `develop`.

### 🧹 Other practices

- Use conventional commit messages (e.g., `feat: add new combat mechanic`).
- Check your code with linters and static analysis tools (we provide configs).
- Update relevant documentation if needed.

## 💬 Questions?

Si quelque chose n'est pas clair, merci de demander sur le Discord de l'équipe ou d'ouvrir une discussion dans le dépôt.

---

## 🇫🇷 Guide de contribution

Merci d’envisager de contribuer à **Ashes of Velsingrad** !

Nous sommes une petite équipe indépendante, et nous tenons à garder un code propre, maintenable et cohérent avec notre vision artistique. Merci de lire ces instructions attentivement avant de commencer.

### 💡 Modèle de branches

Nous utilisons **trois branches principales** :

- `develop` : branche principale de travail. Toutes les features et correctifs commencent ici.
- `canary` : branche bêta. On merge `develop` dans `canary` quand on veut tester les fonctionnalités en contexte pré-release.
- `release` : branche finale prête à publier. On merge seulement `canary` dans `release` quand tout est stable et validé.

**Ne jamais pousser directement sur `canary` ou `release` !**

### 🚀 Comment contribuer

1️⃣ **Forker et cloner le dépôt**

```bash
git clone <url de votre fork>
cd ashes-of-velsingrad
```

2️⃣ **Créer une branche feature à partir de `develop`**

```bash
git checkout develop
git checkout -b feat/nom-de-votre-feature
```

3️⃣ **Respecter les standards**

- Utiliser l’anglais pour les commentaires et identifiants.
- Commits petits et explicites (éviter les gros commits "dump").
- Ajouter des tests si possible.
- Bien documenter les nouvelles fonctions ou modules.

4️⃣ **Soumettre un pull request (PR)**

- Cibler **`develop`**.
- Ajouter une description claire : quoi, pourquoi, captures d’écran ou logs si nécessaire.
- Référencer les issues (ex: `Fixes #42`).

5️⃣ **Review**

- Un membre de l’équipe relit votre PR.
- Apporter les corrections demandées avant merge.
- Une fois approuvée, la PR est mergée dans `develop`.

### 🧹 Autres bonnes pratiques

- Utiliser des messages de commit conventionnels (ex: `feat: ajout d’une mécanique de combat`).
- Vérifier le code avec les linters et outils d’analyse statique (configs fournies).
- Mettre à jour la documentation concernée.

## 💬 Des questions?

Si quelque chose n'est pas clair, merci de demander sur le Discord de l'équipe ou d'ouvrir une discussion dans le dépôt.
