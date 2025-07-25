#### Mono Patcher console plugin for The Sims 3
This project aims to improve the debugging experience for The Sims 3 modders by providing a command-line console.
  
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

4. Import `console.cs` into your solution to use the console functionality.

## Usage in your code
* **`Console.Writeline("string")`**: Prints a message to the native console window.

* **`Console.StartLogging("filename")`**: Starts logging all console output to a file.
  
* **`Console.StopLogging("filename")`**: Stops logging to the specified log file.

* **`Console.Close()`**: Closes the console window and stops all logging.

* **`Console.Create()`**: Manually creates the console window (if not already opened).

## Licenses
This project is licensed under the **GNU General Public License v3.0 (GPLv3)**.
