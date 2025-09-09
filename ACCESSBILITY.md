# Accessibility Checklist – Ashes of Velsingrad

**Goal:** Start thinking about accessibility from the beginning and track progress.

This document outlines the accessibility features of Ashes of Velsingrad, what is implemented, and what is planned for the future. Accessibility is an ongoing process; contributions and feedback are welcome.

**Project Status:** Early Development | **Engine:** Godot (C#)

---

## 📄 Documentation

- **Purpose:** Track accessibility features, current implementation, and future tasks.
- **Reference Guidelines:** [WCAG 2.2](https://www.w3.org/WAI/standards-guidelines/wcag/)

---

## 🖱️ Keyboard Navigation

- [ ] All interactive elements accessible with keyboard only.
- [ ] Visible focus indicators on all buttons, links, and menu items.
- [ ] Keyboard shortcuts tested for menus, dialogs, and key actions.

**Current Status:**  
Nothing implemented yet. Standard Godot input handling is in place but no accessibility-specific keyboard navigation features have been added.

**TODO:**  
- Implement focus management system using Godot's `grab_focus()` and focus neighbors
- Add visible focus indicators to all UI elements (buttons, menus, input fields)
- Create keyboard navigation flow for main menu, settings, inventory, and combat UI
- Implement Tab/Shift+Tab navigation through UI elements
- Add Enter/Space key activation for focused buttons
- Test keyboard-only navigation through all game screens
- Implement escape key handling for closing dialogs and menus

---

## 🔠 Text and Typography

- [x] Adjustable text size without breaking layout.
- [ ] Relative font units (em/rem) used throughout UI.
- [ ] Clear, readable fonts chosen for main text and menus.

**Current Status:**  
- Text scaling function implemented and accessible through game settings
- Basic font sizing system in place

**TODO:**  
- Refactor Settings UI to follow accessibility best practices for text scaling controls
- Replace hardcoded font sizes with relative scaling system throughout codebase
- Implement minimum font size constraints (recommend 16px base minimum)
- Add text scaling preview in settings menu
- Ensure UI layouts remain functional at 200% text scale
- Test text scaling with different screen resolutions
- Choose and implement dyslexia-friendly font options
- Add font weight options for improved readability

---

## 🎨 Visual Contrast (Priority)

- [ ] High contrast between text and background.
- [ ] Color combinations tested for readability.
- [ ] Information is not conveyed by color alone.

**Current Status:**  
Nothing implemented yet. Using default Godot theme colors and standard UI elements without accessibility considerations.

**TODO:**  
- Audit all UI elements for WCAG AA contrast compliance (4.5:1 for normal text, 3:1 for large text)
- Implement high contrast theme option in settings
- Add colorblind-friendly palette alternatives
- Review and redesign health/mana bars, status indicators, and UI elements to not rely solely on color
- Test color combinations with colorblind simulation tools
- Add texture/pattern alternatives for color-coded information (enemy types, item rarities, etc.)
- Implement focus indicators with sufficient contrast
- Create dark mode/light mode theme options
- Test all visual elements under different lighting conditions

---

## 🖥️ Screen Reader Support (Future)

- [ ] Descriptive labels for buttons, links, and UI elements.
- [ ] Semantic headings and structures.
- [ ] Alt text for all images and icons.

**Current Status:**  
Nothing implemented yet. Godot's accessibility features for screen readers have not been explored or implemented.

**TODO:**  
- Research Godot's accessibility node system and screen reader compatibility
- Add descriptive names to all Control nodes for screen reader identification
- Implement accessible descriptions for combat actions and game state changes
- Create audio cues for important game events (level up, item pickup, enemy encounters)
- Add screen reader announcements for menu navigation
- Implement accessible inventory and character sheet descriptions
- Test with NVDA, JAWS, and other popular screen readers (when available)
- Create fallback text descriptions for visual-only information

---

## 🎮 Game-Specific Accessibility (Future)

- [ ] Subtitle system for all audio content
- [ ] Audio cue alternatives for visual indicators
- [ ] Difficulty/complexity options for cognitive accessibility
- [ ] Motor accessibility options (hold-to-toggle, timing adjustments)

**Current Status:**  
Nothing implemented yet. Standard game mechanics without accessibility considerations.

**TODO:**  
- Implement comprehensive subtitle system with speaker identification
- Add visual indicators for audio cues (combat sounds, environmental audio)
- Create simplified UI mode for cognitive accessibility
- Add timing adjustment options for time-sensitive actions
- Implement one-handed control schemes
- Add pause-anywhere functionality
- Create customizable control mapping system
- Test with various input devices and assistive technologies

---

## ✅ Tracking Progress

**Implemented Features:**  
- Basic text scaling functionality in game settings

**In Development:**  
- Settings UI improvements for accessibility options

**Pending Features / TODO (by Priority):**
1. **High Priority (Visual Deficiencies Focus):**
   - Visual contrast audit and improvements
   - High contrast theme implementation
   - Colorblind-friendly design updates
   - Text scaling system refinements

2. **Medium Priority:**
   - Comprehensive keyboard navigation
   - Font and typography improvements
   - Audio alternatives for visual information

3. **Future Implementation:**
   - Screen reader support research and implementation
   - Motor accessibility features
   - Cognitive accessibility options

---

## 🔧 Technical Implementation Notes

**Godot-Specific Considerations:**
- Utilize Godot's `AccessibilityNode` system when implementing screen reader support
- Leverage `Control.focus_mode` and focus neighbor properties for keyboard navigation
- Use Godot's theme system for implementing high contrast and font scaling options
- Consider Godot's input map system for customizable controls

**Development Reminders:**
- Test accessibility features on different platforms (Windows, Linux, Mac)
- Document accessibility API usage for future developers
- Create accessibility testing checklist for each game update
- Plan for localization impact on accessibility features

---

## 📌 Links

- [Main README](../README.md) – Include a link to this accessibility document
- [WCAG Guidelines](https://www.w3.org/WAI/standards-guidelines/wcag/)
- [Godot Accessibility Documentation](https://docs.godotengine.org/en/stable/tutorials/ui/gui_accessibility.html)
- [Accessibility Feedback](#) – Add link for community feedback when available

---

> **Note for Contributors/Consultants:** This project is in early development with basic accessibility groundwork. We welcome feedback on implementation priorities and technical approaches. Accessibility is a continuous effort, and we're committed to making Ashes of Velsingrad accessible to everyone.
