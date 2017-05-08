using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using log4net;
using VSCodeDebug;
using WinDbgDebug.WinDbg;

namespace WinDbgDebug
{
    internal class Program
    {
        private static CommandLineOptions _options = new CommandLineOptions();
        private static ILog _logger;

        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

#if DEBUG
            Debugger.Launch();
#endif
            if (!CommandLine.Parser.Default.ParseArguments(args, _options))
            {
                Console.Error.WriteLine(_options.GetUsage());
                return (int)ReturnCodes.IncorrectArgumentsProvided;
            }

            Logging.Configure(_options.Verbosity);
            _logger = LogManager.GetLogger("WinDbgDebugger");

            if (!string.IsNullOrWhiteSpace(_options.CurrentDirectory)
                && Directory.Exists(_options.CurrentDirectory))
            {
                Environment.CurrentDirectory = _options.CurrentDirectory;
            }

            if (_options.Port != -1)
            {
                _logger.Debug($"Starting to listen from network.");
                RunServer(Utilities.FindFreePort(_options.Port));
            }
            else
            {
                _logger.Debug($"Starting to listen from console.");
                RunSession(Console.OpenStandardInput(), Console.OpenStandardOutput());
            }

            return (int)ReturnCodes.OK;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Fatal($"Fatal exception occured: {e.ExceptionObject} ", e.ExceptionObject as Exception);
        }

        private static void RunSession(Stream inputStream, Stream outputStream)
        {
            DebugSession debugSession = new WinDbgDebugSession(_options.TraceRequests, _options.TraceResponses);
            debugSession.Start(inputStream, outputStream).Wait();
        }

        private static void RunServer(int port)
        {
            _logger.Debug($"Listening on port \"{port}\"");
            TcpListener serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            serverSocket.Start();

            new System.Threading.Thread(() =>
            {
                while (true)
                {
                    var clientSocket = serverSocket.AcceptSocket();
                    if (clientSocket != null)
                    {
                        _logger.Debug(">> accepted connection from client");

                        new System.Threading.Thread(() =>
                        {
                            using (var networkStream = new NetworkStream(clientSocket))
                            {
                                try
                                {
                                    RunSession(networkStream, networkStream);
                                }
                                catch (Exception e)
                                {
                                    _logger.Error($"Error occured during debug session: {e.Message}", e);
                                }
                            }
                            clientSocket.Close();
                            _logger.Debug(">> client connection closed");
                        }).Start();
                    }
                }
            }).Start();
        }
    }
}
