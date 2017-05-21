#tool "nuget:?package=NUnit.ConsoleRunner"

var target = Argument("target", "Test");
var rustInstallerPath = System.IO.Path.Combine(Environment.CurrentDirectory, "rustup.exe");

Task("Install-Rust")
    .Does(() => 
    {
        System.Net.ServicePointManager.SecurityProtocol =
            System.Net.SecurityProtocolType.Tls
            | System.Net.SecurityProtocolType.Ssl3  
            | System.Net.SecurityProtocolType.Tls12;
        var client = new System.Net.WebClient();
        client.DownloadFile("https://win.rustup.rs/", rustInstallerPath);
        using (var process = StartAndReturnProcess(rustInstallerPath, new ProcessSettings { Arguments = "install stable-i686-pc-windows-msvc" }))
        {
            process.WaitForExit();
        }

        using (var process = StartAndReturnProcess(rustInstallerPath, new ProcessSettings { Arguments = "install stable-x86_64-pc-windows-msvc" }))
        {
            process.WaitForExit();
        }

        using (var process = StartAndReturnProcess(rustInstallerPath, new ProcessSettings { Arguments = "component add rust-src" }))
        {
            process.WaitForExit();
        }
    })
    .OnError(ex => Information(ex.ToString()));

Task("Restore-Packages")
    .Does(() => 
    {
        var solutions = GetFiles("../**/*.sln");
        // Restore all NuGet packages.
        foreach(var solution in solutions)
        {
            Information("Restoring {0}", solution);
            NuGetRestore(solution);
        }
    });

Task("Build-Debugger")
    .IsDependentOn("Restore-Packages")
    .Does(() => 
    {
        MSBuild("../src/windbg-debug.sln", new MSBuildSettings 
		{ 
			Configuration = "Release", 
		});
    });

Task("Build-Cpp-Debuggee")
    .Does(() =>
    {
        MSBuild("../src/windbg-debug-tests/test-debuggees/cpp/src/CppDebuggee.sln",
        new MSBuildSettings {
            Configuration = "Debug",
            MSBuildPlatform = MSBuildPlatform.x64,   
        });
    });

Task("Build-Rust-Debuggee")
    .IsDependentOn("Install-Rust")
    .Does(() =>
    {
        using (var process = StartAndReturnProcess(
            rustInstallerPath, 
            new ProcessSettings { Arguments = "default stable-x86_64-pc-windows-msvc", WorkingDirectory = "../src/windbg-debug-tests/test-debuggees/rust/" }))
            {
                process.WaitForExit();
            }

        using (var process = StartAndReturnProcess(
            "cargo", 
            new ProcessSettings { Arguments = "build --target x86_64-pc-windows-msvc", WorkingDirectory = "../src/windbg-debug-tests/test-debuggees/rust/" }))
            {
                process.WaitForExit();
            }

        using (var process = StartAndReturnProcess(
            rustInstallerPath, 
            new ProcessSettings { Arguments = "default stable-i686-pc-windows-msvc", WorkingDirectory = "../src/windbg-debug-tests/test-debuggees/rust/" }))
            {
                process.WaitForExit();
            }

        using (var process = StartAndReturnProcess(
            "cargo", 
            new ProcessSettings { Arguments = "build --target i686-pc-windows-msvc", WorkingDirectory = "../src/windbg-debug-tests/test-debuggees/rust/" }))
            {
                process.WaitForExit();
            }
    });

Task("Build")
    .IsDependentOn("Build-Debugger")
    .IsDependentOn("Build-Cpp-Debuggee")
    .IsDependentOn("Build-Rust-Debuggee")
    .Does(() => {});
	
Task("Install DbgEng")
	.Does(() => 
	{
		ChocolateyInstall("windbg");
	});
	
Task("Test")
	.IsDependentOn("Build")
	.IsDependentOn("Install DbgEng")
	.Does(() => 
    {
        NUnit3("../bin/windbg-debug-tests.dll");
    });

RunTarget(target);