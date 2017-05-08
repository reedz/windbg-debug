## WinDbg Debug Adapter for Visual Studio Code

[![Build status](https://ci.appveyor.com/api/projects/status/7h69jo2ojn82ltte?svg=true)](https://ci.appveyor.com/project/reedz/windbg-debug)

**WinDbg-debug** (for a lack of a better name) is a debug extension for [Visual Studio Code](https://code.visualstudio.com) that uses WinDbg  engine to debug applications. It is suited for debugging native binaries compiled with MSVC compiler toolchain. Currently supported features include:

* Flow control - step over, step into, step out, pause, continue;
* Local variables with nesting - complex structures can be unfolded;
* Watch window - supports only C-style watches;
* [Rust](https://www.rust-lang.org) visualizers - extensions to prettify rust-compiled structures output in variables window;
* Thread / Stackframe switching.

### Usage

**In order for the extension to work, you need first to have WinDbg installed.**
To achieve this:
* Install [debugging tools](https://chocolatey.org/packages/windbg) using chocolatey package manager, or
* Install [debugging tools][ms-debug-tools] from official source.

After this, the extension is used as any other debugger - by configuring launch.json in Visual Studio Code. For sample configuration, please see [this link](https://github.com/reedz/windbg-debug/blob/master/src/windbg-debug-tests/test-debuggees/rust/.vscode/launch.json).

### Contribution

As multiple issues will likely be uncovered during extensive use of the extension, everyone willing to help are welcome to contribute. For details, please see [contribution guide][contribution-guide].

[ms-debug-tools]: https://msdn.microsoft.com/en-us/library/windows/hardware/ff551063(v=vs.85).aspx
[contribution-guide]: https://github.com/reedz/windbg-debug/blob/master/CONTRIBUTING.md