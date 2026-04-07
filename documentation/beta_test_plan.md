---

title:          Beta Test Plan
subtitle:       Ashes of Velsingrad
author:         Louis Ferrari
module:         G-EIP-700
version:        1.0
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

---

## **3. Feature table**

The following features represent the complete beta scope and will be demonstrated during the Greenlight defense. They are ordered according to a typical player tutorial flow, including exploration and combat phases.

| **Feature ID** | **User role** | **Feature name**                | **Short description**                                                   |
| -------------- | ------------- | ------------------------------- | ----------------------------------------------------------------------- |
| F1             | Player        | Start a new game                | Launch the game and enter the tutorial sequence                         |
| F2             | Player        | Move the player character       | Move freely in an exploration area between combat phases                |
| F3             | Player        | Interact with NPCs              | Talk to NPCs through dialogue interactions                              |
| F4             | Player        | Collect items                   | Pick up items from the environment and add them to the global inventory |
| F5             | Player        | Navigate the tutorial interface | Understand HUD elements, tooltips, and objective indicators             |
| F6             | Player        | Enter a battle                  | Transition from exploration to a tactical combat encounter              |
| F7             | Player        | Select a target                 | Highlight enemies or interactable tiles during combat                   |
| F8             | Player        | Perform a basic attack          | Execute a simple combat action against an enemy                         |
| F9             | Player        | Use an item                     | Use an item from a character inventory during battle                    |
| F10            | Player        | End a turn                      | Confirm actions and pass control to the next turn                       |
| F11            | Player        | Trigger a narrative choice      | Make a dialogue or narrative choice that affects the tutorial flow      |
| F12            | Player        | Receive tutorial feedback       | Display contextual explanations and error messages                      |
| F13            | Player        | Handle character defeat         | Lose a character and receive appropriate feedback or consequences       |
| F14            | Player        | Complete the tutorial           | Finish the tutorial scenario and reach the end screen                   |

---

## **4. Success criteria**

The table below defines how the maturity of the tutorial beta will be evaluated.

| **Feature ID** | **Key success criteria**                                                | **Indicator / metric**                       | **Result**      |
| -------------- | ----------------------------------------------------------------------- | -------------------------------------------- | --------------- |
| F1             | The player can start a new game and access the tutorial without crashes | 10 launches, 0 critical crashes              | To be evaluated |
| F2             | The player can move freely in exploration areas                         | 10 sessions, no blocking collisions          | To be evaluated |
| F3             | NPC dialogue can be triggered and completed                             | 10 interactions, all dialogues readable      | To be evaluated |
| F4             | Collected items appear in the global inventory                          | 10 pickups, 100% visible                     | To be evaluated |
| F5             | Tutorial UI elements are visible and understandable                     | 10 testers, ≥ 80% report clear understanding | To be evaluated |
| F6             | Transition from exploration to battle works correctly                   | 10 transitions, no loading issues            | To be evaluated |
| F7             | Targets can be selected without ambiguity                               | 20 selections, correct highlight each time   | To be evaluated |
| F8             | A basic attack can be executed and resolved correctly                   | 15 attacks, correct damage and animations    | To be evaluated |
| F9             | An item can be selected and used during battle                          | 10 item uses, correct effects applied        | To be evaluated |
| F10            | Ending a turn correctly advances the game state                         | 10 turn cycles, no deadlocks                 | To be evaluated |
| F11            | Narrative choices affect the tutorial path                              | 2 branches tested, both accessible           | To be evaluated |
| F12            | Tutorial messages trigger at the correct time                           | 90% of prompts shown as intended             | To be evaluated |
| F13            | Character defeat is handled without blocking the game                   | 5 defeats, no soft-locks                     | To be evaluated |
| F14            | The tutorial can be completed from start to finish                      | 10 full playthroughs, ≥ 8 completions        | To be evaluated |

---

*This Beta Test Plan intentionally limits its scope to the tutorial only. No advanced systems (progression, full narrative branching, or late-game mechanics) are included at this stage, as they are planned for later development phases.*
