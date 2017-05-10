using System;
using System.IO;
using System.Reflection;

namespace WinDbgDebug
{
    public static class AssemblyHelpers
    {
        public static string ResolveAssemblyDirectory(Assembly assembly)
        {
            var executingDirectoryPath = Path.GetDirectoryName(assembly.Location);

            Uri codeBaseUri;
            if (Uri.TryCreate(assembly.CodeBase, UriKind.RelativeOrAbsolute, out codeBaseUri))
                executingDirectoryPath = Path.GetDirectoryName(codeBaseUri.LocalPath);

            return executingDirectoryPath;
        }
    }
}
