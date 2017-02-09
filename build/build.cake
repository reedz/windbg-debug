var target = Argument("target", "Build");

Task("Install-Rust")
    .Does(() => 
    {
        System.Net.ServicePointManager.SecurityProtocol =
            System.Net.SecurityProtocolType.Tls
            | System.Net.SecurityProtocolType.Ssl3  
            | System.Net.SecurityProtocolType.Tls12;
        var client = new System.Net.WebClient();
        var installerPath = System.IO.Path.Combine(Environment.CurrentDirectory, "rustup.exe");
        client.DownloadFile("https://win.rustup.rs/", installerPath);
        using (var process = StartAndReturnProcess(installerPath, new ProcessSettings { Arguments = "-y" }))
        {
            process.WaitForExit();
        }
    })
    .OnError(ex => Information(ex.ToString()));

Task("Build-Debugger")
    .Does(() => 
    {
        MSBuild("../windbg-debug.sln");
    });

Task("Build-Cpp-Debuggee")
    .Does(() =>
    {
        MSBuild("../windbg-debug-tests/test-debuggees/cpp/src/CppDebuggee.sln");
    });

Task("Build-Rust-Debuggee")
    .IsDependentOn("Install-Rust")
    .Does(() =>
    {
        using (var process = StartAndReturnProcess(
            "cargo", 
            new ProcessSettings { Arguments = "build", WorkingDirectory = "../windbg-debug-tests/test-debuggees/rust/" }))
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


