# GridMap Editor Tool (Godot Plugin)

A simple tool to place and visualize player and enemy spawners directly inside a GridMap.

---

## 🚀 Installation

1. Go to **Project → Project Settings → Plugins**
2. Enable **GridMapEditorTool**

---

## 📦 Requirements

Your scene must contain:

- A **GridMap**
- Two required child nodes:
  - `HoveredCell`
  - `SpawnerVisualizer`

> ⚠️ The plugin will not work without these nodes.

---

## ⚙️ How It Works

### HoveredCell

- Automatically updates its name to match the hovered cell position (`Vector3I`)

### SpawnerVisualizer

- Displays spawners in the editor

---

## 🧩 Usage

1. Select either:
   - `HoveredCell`
   - `SpawnerVisualizer`

2. Hover a cell in the GridMap

3. Click on the cell → a popup appears

4. Behavior depends on the cell state:

   **If the cell is empty:**
   - ➕ **Add Player Spawner**
   - ➕ **Add Enemy Spawner**

   **If the cell already contains a spawner:**
   - ❌ **Remove Spawner**

---

## 💾 Data Storage

- A `.tres` file is automatically created:
  - Same name as the scene
  - Same folder

- It stores:
  - All spawners
  - Total number of spawners

---

## 🎨 Visual Feedback

- 🟦 Blue square → Player spawner  
- 🟥 Red square → Enemy spawner  

---

## 🛠️ Troubleshooting

**Nothing happens or visuals don’t update?**

- Save the scene
- Reload the editor

---

## ✨ Tips

- Always keep `HoveredCell` and `SpawnerVisualizer` as children of the GridMap
- Make sure one of them is selected when interacting with the grid
