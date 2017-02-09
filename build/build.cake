var target = Argument("target", "Build");
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
        using (var process = StartAndReturnProcess(rustInstallerPath, new ProcessSettings { Arguments = "default stable-msvc" }))
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
        MSBuild("../windbg-debug.sln", new MSBuildSettings {
            Configuration = "Release",
        });
    });

Task("Build-Cpp-Debuggee")
    .Does(() =>
    {
        MSBuild("../windbg-debug-tests/test-debuggees/cpp/src/CppDebuggee.sln",
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
            new ProcessSettings { Arguments = "run stable-msvc cargo build", WorkingDirectory = "../windbg-debug-tests/test-debuggees/rust/" }))
            {
                process.WaitForExit();
            }
    });

Task("Build")
    .IsDependentOn("Build-Debugger")
    .IsDependentOn("Build-Cpp-Debuggee")
    .IsDependentOn("Build-Rust-Debuggee")
    .Does(() => {});

RunTarget(target);


