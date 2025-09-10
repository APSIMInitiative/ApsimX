# Selenium UI Tests for APSIM-X MainView

This directory contains Selenium-based UI tests for the APSIM-X MainView component. Since APSIM-X is a GTK desktop application, these tests use a hybrid approach to validate the menu structure that would be accessible through Selenium if it were a web application.

## Test Files

### MainViewSeleniumTests.cs
Full Selenium WebDriver tests that:
- Generate a temporary HTML representation of the MainMenu structure
- Use Chrome WebDriver to load and test the HTML page
- Validate that exactly 8 menu items are present
- Verify all expected menu item names are found

**Note**: These tests require Chrome and ChromeDriver to be available. In CI environments without browser support, they will be marked as inconclusive.

### MainViewSeleniumCompatibilityTests.cs  
Browser-independent tests that:
- Generate Selenium-compatible HTML content representing the menu structure
- Validate HTML structure that Selenium would interact with
- Test CSS selectors and element discovery patterns
- Verify proper HTML document generation

### MainMenuReflectionTests.cs
Core validation tests that:
- Use reflection to find all `[MainMenu]` attributes in the MainMenu class
- Verify exactly 8 menu methods exist
- Validate menu item names and keyboard shortcuts
- Test the underlying data that drives the UI tests

## Running the Tests

```bash
# Run all menu-related tests
dotnet test --filter "MainMenu"

# Run only reflection tests (no browser required)  
dotnet test --filter "MainMenuReflectionTests"

# Run Selenium compatibility tests (no browser required)
dotnet test --filter "MainViewSeleniumCompatibilityTests"

# Run full Selenium tests (requires Chrome)
dotnet test --filter "MainViewSeleniumTests"
```

## Test Approach

The tests validate that the MainView contains exactly **8 menu options**:

1. **Save** (`Ctrl+S`)
2. **Save As** (`Ctrl+Shift+S`)
3. **Undo** (`Ctrl+Z`)
4. **Redo** (`Ctrl+Y`)
5. **Split Screen**
6. **Clear Status** (`Ctrl+G`)
7. **Help** (`F1`)
8. **Run** (`F5`)

These menu items are defined in `MainMenu.cs` using the `[MainMenu]` attribute and are reflected in the MainView UI.

## Demo Script

Run `./demo_selenium_test.sh` to see a demonstration of what the Selenium tests validate without requiring a browser setup.