﻿using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using DuetControlServer.Commands;

namespace DuetControlServer.IPC.Processors
{
    /// <summary>
    /// Command interpreter for client requests
    /// </summary>
    public sealed class Command : Base
    {
        /// <summary>
        /// List of supported commands in this mode
        /// </summary>
        public static readonly Type[] SupportedCommands =
        {
            typeof(GetFileInfo),
            typeof(ResolvePath),
            typeof(Code),
            typeof(EvaluateExpression),
            typeof(Flush),
            typeof(SimpleCode),
            typeof(WriteMessage),
            typeof(AddHttpEndpoint),
            typeof(RemoveHttpEndpoint),
            typeof(CheckPassword),
            typeof(GetObjectModel),
            typeof(LockObjectModel),
            typeof(PatchObjectModel),
            typeof(SetObjectModel),
            typeof(SetUpdateStatus),
            typeof(SyncObjectModel),
            typeof(UnlockObjectModel),
            typeof(InstallPlugin),
            typeof(ReloadPlugin),
            typeof(SetNetworkProtocol),
            typeof(SetPluginData),
            typeof(SetPluginProcess),
            typeof(StartPlugin),
            typeof(StartPlugins),
            typeof(StopPlugin),
            typeof(StopPlugins),
            typeof(UninstallPlugin),
            typeof(AddUserSession),
            typeof(RemoveUserSession),
            typeof(InstallSystemPackage),
            typeof(UninstallSystemPackage)
        };

        /// <summary>
        /// Static constructor of this class
        /// </summary>
        static Command() => AddSupportedCommands(SupportedCommands);

        /// <summary>
        /// Logger instance
        /// </summary>
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Constructor of the command interpreter
        /// </summary>
        /// <param name="conn">Connection instance</param>
        public Command(Connection conn) : base(conn) => _logger.Debug("Command processor added for IPC#{0}", conn.Id);

        /// <summary>
        /// Reads incoming command requests and processes them. See <see cref="DuetAPI.Commands"/> namespace for a list
        /// of supported commands. The actual implementations can be found in <see cref="Commands"/>.
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public override async Task Process()
        {
            do
            {
                DuetAPI.Commands.BaseCommand command = null;
                try
                {
                    // Read another command from the IPC connection
                    command = await Connection.ReceiveCommand();
                    Type commandType = command.GetType();

                    // Make sure it is actually supported and permitted
                    if (!SupportedCommands.Contains(commandType))
                    {
                        throw new ArgumentException($"Invalid command {command.Command} (wrong mode?)");
                    }
                    Connection.CheckPermissions(commandType);

                    // Execute it and send back the result
                    object result = await command.Invoke();
                    await Connection.SendResponse(result);

                    // Shut down the socket if this was the last command
                    if (Program.CancellationToken.IsCancellationRequested)
                    {
                        Connection.Close();
                    }
                }
                catch (SocketException)
                {
                    // Connection has been terminated
                    break;
                }
                catch (Exception e)
                {
                    // Send errors back to the client
                    if (e is not OperationCanceledException)
                    {
                        if (command != null)
                        {
                            if (e is UnauthorizedAccessException)
                            {
                                _logger.Error("IPC#{0}: Insufficient permissions to execute {1}", Connection.Id, command.Command);
                            }
                            else
                            {
                                _logger.Error(e, "IPC#{0}: Failed to execute {1}", Connection.Id, command.Command);
                            }
                        }
                        else
                        {
                            _logger.Error(e, "IPC#{0}: Failed to receive command", Connection.Id);
                        }
                    }
                    await Connection.SendResponse(e);
                }
            }
            while (!Program.CancellationToken.IsCancellationRequested);
        }
    }
}
