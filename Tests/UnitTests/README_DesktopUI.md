# Desktop UI Testing for APSIM-X MainView

This directory contains desktop UI tests for the APSIM-X MainView using industry-standard testing approaches appropriate for GTK desktop applications.

## Overview

APSIM-X is a GTK#-based desktop application, not a web application. Therefore, proper desktop UI testing approaches are used instead of web-based tools like Selenium.

## Testing Approaches Used

### 1. Component-Level Desktop UI Testing
- **Appropriate technology**: Tests desktop UI components at the class level
- **GTK-aware**: Works with GTK# components and interfaces
- **CI-friendly**: Runs in headless environments without requiring GUI
- **Comprehensive**: Validates UI structure, event handlers, and integration points

### 2. GTK Widget Testing
- **Native approach**: Tests GTK widgets directly
- **Lightweight**: Doesn't require full application launch
- **Unit-level**: Tests individual components and their structure
- **Integration-friendly**: Works in CI environments

### 3. Reflection-Based Validation
- **Data validation**: Ensures menu structure is properly defined
- **Attribute testing**: Validates MainMenu attributes and structure
- **Always available**: No dependencies on GUI or runtime environment

## Test Classes

### MainViewDesktopUITests.cs
Component-level desktop UI validation tests:
- Tests MainView class structure and GTK integration
- Validates menu structure designed for desktop interaction
- Checks keyboard shortcuts follow desktop conventions
- Verifies event handler signatures for proper GTK integration
- Works in CI environments without requiring application launch

```bash
# Run all desktop UI validation tests
dotnet test --filter "MainViewDesktopUITests"

# Run lightweight GTK widget tests
dotnet test --filter "MainViewGtkWidgetTests"

# Run reflection-based tests
dotnet test --filter "MainMenuReflectionTests"

# Run all menu-related tests
dotnet test --filter "MainMenu or MainView"
```

### MainViewGtkWidgetTests.cs
GTK widget-level tests without full application launch:
- Tests GTK component instantiation
- Validates MainMenu class structure and attributes
- Checks keyboard shortcut definitions
- Verifies event handler signatures
- Works in CI environments without GUI

```bash
# Run lightweight GTK widget tests
dotnet test --filter "MainViewGtkWidgetTests"
```

### MainMenuReflectionTests.cs
Core validation using .NET reflection:
- Discovers all `[MainMenu]` attribute decorations
- Validates exactly 8 menu methods exist
- Verifies menu names and keyboard shortcuts
- Provides underlying data validation

```bash
# Run reflection-based tests
dotnet test --filter "MainMenuReflectionTests"
```

## Validated Menu Structure

The tests confirm that MainView contains exactly **8 menu options**:

1. **Save** (`Ctrl+S`)
2. **Save As** (`Ctrl+Shift+S`) 
3. **Undo** (`Ctrl+Z`)
4. **Redo** (`Ctrl+Y`)
5. **Split Screen** (no shortcut)
6. **Clear Status** (`Ctrl+G`)
7. **Help** (`F1`)
8. **Run** (`F5`)

## Running Tests

### Prerequisites
- .NET 8.0 SDK
- GTK# development libraries (for widget tests)

### Commands

```bash
# Run all menu-related tests
dotnet test --filter "MainMenu or MainView"

# Run only lightweight tests (CI-friendly)
dotnet test --filter "MainMenuReflectionTests or MainViewGtkWidgetTests or MainViewDesktopUITests"

# Run specific test categories
dotnet test --filter "MainViewDesktopUITests"
```

## CI/CD Integration

The testing approach provides comprehensive validation that works in all environments:

1. **Reflection tests**: Always run, no dependencies
2. **GTK widget tests**: Run when GTK is available  
3. **Component-level tests**: Test desktop UI structure without requiring running application
4. **Cross-platform**: Works on Linux, Windows, and macOS

This ensures tests can run in various environments while providing thorough desktop UI validation.

## Benefits Over Web-Based Testing

1. **Appropriate technology**: Uses approaches designed for desktop applications
2. **Native validation**: Tests actual GTK components and desktop UI patterns
3. **Performance**: Direct component access instead of browser simulation
4. **Reliability**: No browser dependencies or web rendering issues
5. **Maintenance**: Tests evolve with desktop application structure
6. **CI-friendly**: Runs in headless environments without GUI requirements

## Framework Comparison

| Approach | Use Case | Pros | Cons |
|----------|----------|------|------|
| Component Testing | Desktop UI structure | Fast, comprehensive, CI-friendly | No runtime UI interaction |
| GTK Widget Testing | Widget validation | Lightweight, GTK-native | Limited to widget structure |
| Reflection Testing | Data validation | Always available | No actual UI testing |

## Best Practices

1. **Appropriate testing level**: Use component-level tests for desktop UI structure validation
2. **Environment awareness**: Handle missing dependencies gracefully
3. **Focused testing**: Test specific UI behaviors and integration points
4. **Maintainable assertions**: Use stable UI elements and class structures
5. **Cross-platform compatibility**: Ensure tests work in various environments

This approach provides robust, maintainable desktop UI testing that follows industry standards for GTK application testing while being practical for CI/CD environments.