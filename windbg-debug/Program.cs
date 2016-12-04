using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windbg_debug
{
    class Program
    {
        static int Main(string[] args)
        {
            var options = new CommandLineOptions();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine(options.GetUsage());
                Console.ReadKey();
                return (int)ReturnCodes.IncorrectArgumentsProvided;
            }

            return (int)ReturnCodes.OK;
        }
    }
}
