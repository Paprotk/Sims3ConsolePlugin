# Mono Patcher console plugin for The Sims 3
This project aims to improve the debugging experience for The Sims 3 modders by providing a command-line console.

**Quick Navigation**: [Methods](#methods) | [Cheats](#cheats) | [Logging](#logging)

## Requirements

-  **The Sims 3**:  version **1.67**, expected to work on **1.69** as well

-  **MonoPatcher**: version **0.3.0 or newer**  
- Requires **ASI installed**

- This plugin does not work on macOS, only Windows.

More info here:  
 [MonoPatcher Plugin Development Guide](https://github.com/LazyDuchess/MonoPatcher/wiki/MonoPatcher-Plugin-Development)

## Setup

1. Download the latest release from the [Releases page](https://github.com/Paprotk/Sims3ConsolePlugin/releases).

2. Inside the downloaded archive, you will find two files:
   - `Sims3ConsolePlugin.dll`
   - `Console.cs`

3. Place `Sims3ConsolePlugin.dll` into your game directory at:  
   `\The Sims 3\Game\Bin\MonoPatcher\plugins`

4. Import `Console.cs` into your solution to use the console functionality.

5. Allow unsafe code in your project properties.

# Usage in your code
The included Console.cs file uses preprocessor directives (#if DEBUG) to automatically switch between the real console implementation and a dummy (no-op) fallback class, depending on your build type.

* When you build in Debug configuration:
The real console class is used and this requires that Sims3ConsolePlugin.dll is present in:
`\The Sims 3\Game\Bin\MonoPatcher\plugins` - without it game will crash.

* When you build in not Debug (#if !DEBUG) configuration:
A dummy console class is used that does nothing, preventing game crashes if the DLL is missing.
This allows you to leave Console.* calls in your code, without needing to remove or comment them before release.

The in-game cheats are automatically registered when any Console method is first called. This happens because the static constructor of the Console class executes when any static member is accessed.

#### Note: Closing the console window using the X button will also terminate the game.

<a id="methods"></a>
## Methods
* **`Console.Writeline("string")`**: Prints a message to the native console window.

* **`Console.StartLogging("filename")`**: Starts logging all console output to a file.
  
* **`Console.StopLogging("filename")`**: Stops logging to the specified log file.

* **`Console.Close()`**: Closes the console window and stops all logging.

* **`Console.Create()`**: Manually creates the console window (if not already opened).

* **`Console.Clear()`**: Clears the console buffer and corresponding console window of display information.

* **`Console.Beep()`**: Plays the sound of a beep through the console speaker.

<a id="cheats"></a>
## Cheats
You can also use in-game command console to run command-line functions.
* **`ConsoleWriteLine`**: Prints a message to the native console window.

* **`ConsoleStartLogging <filename>`**: Starts logging all console output to a file.
  
* **`ConsoleStopLogging <filename>`**: Stops logging to the specified log file.

* **`ConsoleClose`**: Closes the console window and stops all logging.

* **`ConsoleCreate`**: Manually creates the console window (if not already opened).

* **`ConsoleClear`**: Clears the console buffer and corresponding console window of display information.

* **`ConsoleBeep`**: Plays the sound of a beep through the console speaker.

<a id="logging"></a>
## Logging
When you call `Console.StartLogging("filename")` or use in-game command `ConsoleStartLogging <filename>`, all console output will be written to a .log file automatically.
Log files are stored in a Logs folder inside `\The Sims 3\Game\Bin\`.
The filename is timestamped to avoid overwriting previous logs.
Remember to call `Console.StopLogging("filename")` or use `ConsoleStopLogging <filename>`, after you finish logging to properly close the log file and flush any remaining output.
The console window title dynamically updates to show currently active loggers, providing visual feedback about logging status.

## Licenses
This project is licensed under the **GNU General Public License v3.0 (GPLv3)**.
