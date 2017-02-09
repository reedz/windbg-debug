## WinDbg Debug Adapter for Visual Studio Code

[![Build status](https://ci.appveyor.com/api/projects/status/7h69jo2ojn82ltte?svg=true)](https://ci.appveyor.com/project/reedz/windbg-debug)

**WinDbg-debug** (for a lack of a better name) is a debug extension for [Visual Studio Code](https://code.visualstudio.com) that uses WinDbg  engine to debug applications. It is suited for debugging native binaries compiled with MSVC compiler toolchain. Currently supported features include:

* Flow control - step over, step into, step out, pause, continue;
* Local variables with nesting - complex structures can be unfolded;
* Watch window - supports only C-style watches;
* [Rust](https://www.rust-lang.org) visualizers - extensions to prettify rust-compiled structures output in variables window;
* Thread / Stackframe switching.

### Installation

Extension is currently not published yet.
To try it out, please use the following steps:

1. Download or clone github repository;
2. Open repository root in Visual Studio Code;
3. Switch to *Debug* panel and run "Extension host" configuration

New VS Code window should open with extension installed. You may now proceed to opening and debugging native windows projects in VS Code.

To install this extension, follow the steps below:

1. Install extension from VS code marketplace: _link pending_
2. Install "Debugging tools for Windows": [installation guide](https://msdn.microsoft.com/en-us/library/windows/hardware/ff551063(v=vs.85).aspx)

### Usage



