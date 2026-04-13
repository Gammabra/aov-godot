---

title:          Beta Test Plan
subtitle:       Ashes of Velsingrad
author:         Eric XU
module:         G-EIP-700
version:        1.1
---

## **1. Project context**

*Ashes of Velsingrad* is a tactical RPG developed with Godot, combining HD 2D characters in a 3D environment. The game features turn-based tactical combat, narrative choices, and character progression.

The objective of this beta version is not to present the full game, but to validate the tutorial sequence, which introduces players to the core mechanics of the game. The tutorial is designed to ensure that new players understand the basic controls, tactical combat rules, user interface, and interaction systems before accessing the rest of the game.

This beta focuses on a guided first-play experience, where players complete a short playable scenario with contextual explanations, step-by-step objectives, and limited available actions. The goal is to verify that the tutorial is understandable, functional, and stable under real player conditions.

---

## **2. User roles**

The following roles will be involved in the beta test.

| **Role Name** | **Description**                                                                                             |
| ------------- | ----------------------------------------------------------------------------------------------------------- |
| Player        | A tester playing the tutorial for the first time, discovering the controls, combat mechanics, and interface |
| Companion NPC | Young Mercenary guiding the player (tutorial + combat support)                                              |
| Ally NPC      | Soldiers or villagers assisting during combat                                                               |
| Enemies       | Bandits encountered in the first combat                                                                     |
| System        | Handles core game logic such as turn management, combat flow, rule enforcement, and game state updates      |

---

## **3. Feature table**

The following features represent the complete beta scope and will be demonstrated during the Greenlight defense. They are ordered according to a typical player tutorial flow, including exploration and combat phases.

| **Feature ID** | **User role**                                 | **Feature name**                             | **Short description**                                                                            |
| -------------- | --------------------------------------------- | -------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| F1             | Player                                        | Start a new game                             | Launch the game and enter the tutorial sequence                                                  |
| F2             | Player                                        | Hear music and sound effects                 | Background music and sound effects play during gameplay                                          |
| F3             | Player                                        | See visual effects                           | See visual effects on each actions or events during gamelplay                                    |
| F4             | Player                                        | Move the player character                    | Move freely in an exploration area between combat phases                                         |
| F5             | Player                                        | Interact with NPCs                           | Talk to NPCs through dialogue interactions                                                       |
| F6             | Player                                        | Collect items                                | Pick up items from the environment and add them to the global inventory                          |
| F7             | Player                                        | Open the inventory                           | Access and view collected items                                                                  |
| F8             | Companion NPC                                 | Explain the inventory                        | Explain how the inventory system works                                                           |
| F9             | Player                                        | Escape the prison                            | Pass to next level                                                                               |
| F10            | Player                                        | Choose a narrative action                    | Decide between helping soldiers or villagers                                                     |
| F11            | Player                                        | Enter a battle                               | Transition from exploration to a tactical combat encounter                                       |
| F12            | Componion NPC                                 | Explain the combat system                    | Provide tutorial guidance explaining combat mechanics and player actions                         |
| F13            | System                                        | Turn-by-turn system                          | Control the order of actions between player and enemies during combat                            |
| F14            | Player + Companion NPC + Allies NPC + Enemies | Move to a tile                               | Move a unit to a selected tile during combat                                                     |
| F15            | Player + Allies NPC                           | Select a target                              | Highlight enemies or interactable tiles during combat                                            |
| F16            | Player + Allies NPC                           | Perform a basic attack                       | Execute a simple combat action against an enemy                                                  |
| F17            | Player                                        | Use an item                                  | Use an item from a character inventory during battle                                             |
| F18            | Player                                        | End a turn                                   | Confirm actions and pass control to the next turn                                                |
| F19            | Enemies                                       | Execute enemy AI behavior                    | Enemies automatically choose targets, move, and attack based on decision rules during their turn |
| F20            | Player                                        | Display tutorial success or failure feedback | The game indicates whether the player successfully completes tutorial steps                      |
| F21            | Player                                        | Handle character defeat                      | Lose a character and receive appropriate feedback or consequences                                |
| F22            | Player                                        | Complete the tutorial                        | Finish the tutorial scenario and reach the end screen                                            |

---

## **4. Success criteria**

The table below defines how the maturity of the tutorial beta will be evaluated.

| **Feature ID** | **Key Success Criteria**                                | **Indicator / Metric**                                 | **Result Achieved** |
|--------------- | ------------------------------------------------------- | ------------------------------------------------------ | ------------------- |
| F1             | The game starts and loads the tutorial                  | 10 launches, 0 crashes                                 | To be tested        |
| F2             | Music and SFX play correctly during gameplay            | Audio plays in 80% of sessions, no missing sounds      | To be tested        |
| F3             | Visual effects are displayed during actions             | 15 actions tested, effects triggered each time         | To be tested        |
| F4             | Player can move freely in exploration                   | 10 sessions, no blocking or collision issues           | To be tested        |
| F5             | Dialogue triggers correctly with NPCs                   | 10 interactions, dialogue appears each time            | To be tested        |
| F6             | Items are collected and stored properly                 | 10 pickups, all items appear in inventory              | To be tested        |
| F7             | Inventory opens and displays items                      | Opens in < 1s, correct items shown                     | To be tested        |
| F8             | Inventory tutorial is triggered and understandable      | 90% trigger rate, user understands usage               | To be tested        |
| F9             | Player can exit the prison and reach next level         | 10 runs, level transition works correctly              | To be tested        |
| F10            | Player can make a narrative choice                      | Choice appears in 100% of cases and is selectable      | To be tested        |
| F11            | Combat starts correctly after trigger                   | 10 transitions, no freeze or bug                       | To be tested        |
| F12            | Combat tutorial explains mechanics clearly              | 80%+ users understand basic actions                    | To be tested        |
| F13            | Turn order is respected during combat                   | 10 combats, no turn skips or duplicates                | To be tested        |
| F14            | Units move correctly on the grid                        | 20 moves, valid tiles only, no overlap                 | To be tested        |
| F15            | Player can select valid targets                         | 20 selections, only valid targets selectable           | To be tested        |
| F16            | Basic attacks deal damage correctly                     | 20 attacks, damage applied each time                   | To be tested        |
| F17            | Items can be used during combat                         | 10 uses, correct effect applied                        | To be tested        |
| F18            | Turn ends and switches correctly                        | 20 turns, control passes correctly                     | To be tested        |
| F19            | Enemies act automatically and logically                 | 10 combats, no idle turns, valid actions only          | To be tested        |
| F20            | Tutorial feedback is displayed correctly                | 80% of actions show success/failure feedback           | To be tested        |
| F21            | Character defeat triggers feedback and consequences     | 10 defeats, UI feedback and removal applied            | To be tested        |
| F22            | Tutorial can be completed fully                         | 80% completion rate without blocking bug               | To be tested        |

---

*This Beta Test Plan intentionally limits its scope to the tutorial only. No advanced systems (progression, full narrative branching, or late-game mechanics) are included at this stage, as they are planned for later development phases.*
