using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using VSCodeDebug;
using windbg_debug.WinDbg;

namespace windbg_debug
{
    class Program
    {
        private static CommandLineOptions _options = new CommandLineOptions();
        private static InternalLogger _logger;

        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Debugger.Launch();

            if (!CommandLine.Parser.Default.ParseArguments(args, _options))
            {
                Console.Error.WriteLine(_options.GetUsage());
                return (int)ReturnCodes.IncorrectArgumentsProvided;
            }

            if (!string.IsNullOrWhiteSpace(_options.CurrentDirectory)
                && Directory.Exists(_options.CurrentDirectory))
            {
                Environment.CurrentDirectory = _options.CurrentDirectory;
            }

            _logger = new InternalLogger(_options.Verbose);
            if (_options.Port != -1)
            {
                _logger.Log($"Starting to listen from network.");
                RunServer(Utilities.FindFreePort(_options.Port));
            }
            else
            {
                _logger.Log($"Starting to listen from console.");
                RunSession(Console.OpenStandardInput(), Console.OpenStandardOutput());
            }

            return (int)ReturnCodes.OK;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject);
        }

        private static void RunSession(Stream inputStream, Stream outputStream)
        {
            DebugSession debugSession = new WinDbgDebugSession(_logger, _options.TraceRequests, _options.TraceResponses);
            debugSession.Start(inputStream, outputStream).Wait();
        }

        private static void RunServer(int port)
        {
            TcpListener serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            serverSocket.Start();

            new System.Threading.Thread(() => {
                while (true)
                {
                    var clientSocket = serverSocket.AcceptSocket();
                    if (clientSocket != null)
                    {
                        _logger.Log(">> accepted connection from client");

                        new System.Threading.Thread(() => {
                            using (var networkStream = new NetworkStream(clientSocket))
                            {
                                try
                                {
                                    RunSession(networkStream, networkStream);
                                }
                                catch (Exception e)
                                {
                                    _logger.Log("Exception: " + e);
                                }
                            }
                            clientSocket.Close();
                            _logger.Log(">> client connection closed");
                        }).Start();
                    }
                }
            }).Start();
        }
    }
}
