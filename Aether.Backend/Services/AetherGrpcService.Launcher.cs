using System.Threading.Channels;
using Aether.Protos;
using Aether.PluginSDK;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Aether.Backend.Services;

public partial class AetherGrpcService
{
    public override async Task<LaunchResponse> LaunchGame(LaunchRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("LaunchGame request for Game ID: {GameId} (Admin: {RunAsAdmin})",
                request.GameId, request.RunAsAdmin);

            if (!int.TryParse(request.GameId, out int dbId))
            {
                return new LaunchResponse { Success = false, Message = "Invalid game ID format" };
            }

            var game = _database.GetGameById(dbId);
            if (game == null)
            {
                return new LaunchResponse { Success = false, Message = "Game not found" };
            }

            _logger.LogInformation("Launching {Title} (Platform: {Platform})", game.Title, game.Platform);

            // Build launch context
            var launchContext = new LaunchContext
            {
                GameId = game.Id.ToString(),
                Title = game.Title,
                Platform = game.Platform,
                ExternalId = game.ExternalId ?? "",
                InstallPath = game.InstallPath,
                ExecutablePath = game.ExecutablePath,
                RunAsAdmin = request.RunAsAdmin,
                LaunchArguments = game.LaunchArguments
            };

            // Find appropriate launcher from plugins
            var launchers = new List<IGameLauncher>();
            launchers.AddRange(_pluginManager.GetPlugins().OfType<IGameLauncher>());
            launchers.AddRange(_pluginManager.GetLibraryImporters().OfType<IGameLauncher>());
            launchers = launchers.Distinct().ToList();

            _logger.LogInformation("Found {Count} game launchers", launchers.Count);

            var launcher = launchers.FirstOrDefault(l => l.CanLaunch(launchContext));

            LaunchResult result;
            if (launcher != null)
            {
                _logger.LogInformation("Using launcher: {Name}", (launcher as IPlugin)?.Name ?? "Unknown");
                result = await launcher.LaunchAsync(launchContext);
            }
            else if (!string.IsNullOrEmpty(game.ExecutablePath))
            {
                // Fallback: direct executable launch
                _logger.LogInformation("No plugin launcher found, trying direct executable launch");
                result = LaunchHelper.LaunchExecutable(game.ExecutablePath, request.RunAsAdmin);
            }
            else
            {
                return new LaunchResponse { Success = false, Message = "No launcher available for this game" };
            }

            if (result.Success)
            {
                // Update last played timestamp immediately
                game.LastPlayed = DateTime.UtcNow;
                game.UpdatedAt = DateTime.UtcNow;
                _database.UpsertGame(game);

                _logger.LogInformation("Successfully launched {Title} via {Method}", game.Title, result.LaunchMethod);

                // Start tracking session via GameSessionManager
                _sessionManager.StartSession(game.Id, result);

                // Notify lifecycle hooks
                var gameInfo = new Aether.PluginSDK.Game
                {
                    Id = game.Id.ToString(),
                    Title = game.Title,
                    ExecutablePath = game.ExecutablePath ?? "",
                    Platform = game.Platform
                };

                foreach (var plugin in _pluginManager.GetPlugins())
                {
                    try
                    {
                        await plugin.OnGameLaunched(gameInfo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Plugin {Name} OnGameLaunched hook failed", plugin.Name);
                    }
                }
            }

            return new LaunchResponse
            {
                Success = result.Success,
                Message = result.ErrorMessage ?? "",
                ProcessId = result.ProcessId?.ToString() ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching game {GameId}", request.GameId);
            return new LaunchResponse { Success = false, Message = ex.Message };
        }
    }

    // Deprecated: TrackPlaytimeAsync replaced by GameSessionManager
    // Kept only if needed for legacy logic reference, but removed here for cleanliness

    public override Task<CanLaunchResponse> CanLaunchGame(GameId request, ServerCallContext context)
    {
        try
        {
            if (!int.TryParse(request.Id, out int dbId))
            {
                return Task.FromResult(new CanLaunchResponse
                {
                    CanLaunch = false,
                    Reason = "Invalid game ID format"
                });
            }

            var game = _database.GetGameById(dbId);
            if (game == null)
            {
                return Task.FromResult(new CanLaunchResponse
                {
                    CanLaunch = false,
                    Reason = "Game not found"
                });
            }

            // Build launch context
            var launchContext = new LaunchContext
            {
                GameId = game.Id.ToString(),
                Title = game.Title,
                Platform = game.Platform,
                ExternalId = game.ExternalId ?? "",
                InstallPath = game.InstallPath,
                ExecutablePath = game.ExecutablePath,
                RunAsAdmin = false
            };

            // Find appropriate launcher from plugins
            var launchers = new List<IGameLauncher>();
            launchers.AddRange(_pluginManager.GetPlugins().OfType<IGameLauncher>());
            launchers.AddRange(_pluginManager.GetLibraryImporters().OfType<IGameLauncher>());
            launchers = launchers.Distinct().ToList();

            var launcher = launchers.FirstOrDefault(l => l.CanLaunch(launchContext));

            if (launcher != null)
            {
                var launcherName = (launcher as IPlugin)?.Name ?? "unknown";
                return Task.FromResult(new CanLaunchResponse
                {
                    CanLaunch = true,
                    LaunchMethod = launcherName.ToLower().Replace(" ", "_")
                });
            }

            // Check fallback: direct executable
            if (!string.IsNullOrEmpty(game.ExecutablePath) &&
                (File.Exists(game.ExecutablePath) || Directory.Exists(game.ExecutablePath)))
            {
                return Task.FromResult(new CanLaunchResponse
                {
                    CanLaunch = true,
                    LaunchMethod = "direct"
                });
            }

            return Task.FromResult(new CanLaunchResponse
            {
                CanLaunch = false,
                Reason = "No launcher available for this game"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if game can be launched");
            return Task.FromResult(new CanLaunchResponse
            {
                CanLaunch = false,
                Reason = ex.Message
            });
        }
    }

    public override Task<OperationStatus> StopGame(GameId request, ServerCallContext context)
    {
        if (int.TryParse(request.Id, out int dbId))
        {
            _sessionManager.StopSession(dbId);
            return Task.FromResult(new OperationStatus { Success = true, Message = "Stop command sent" });
        }

        return Task.FromResult(new OperationStatus { Success = false, Message = "Invalid Game ID" });
    }

    public override Task<ActiveProcessesResponse> GetActiveProcesses(GameId request, ServerCallContext context)
    {
        _logger.LogInformation("[gRPC] GetActiveProcesses request for Game ID: {GameId}", request.Id);

        var response = new ActiveProcessesResponse();

        if (int.TryParse(request.Id, out int dbId))
        {
            var processes = _sessionManager.GetTrackedProcesses(dbId);
            _logger.LogDebug("[gRPC] Found {Count} tracked processes for game {Id}", processes.Count, dbId);

            foreach (var p in processes)
            {
                response.Processes.Add(new TrackedProcessInfo
                {
                    ProcessId = p.ProcessId,
                    ProcessName = p.ProcessName ?? "",
                    ExecutablePath = p.ExecutablePath ?? ""
                });
            }
        }
        else
        {
            _logger.LogWarning("[gRPC] Invalid game ID format for GetActiveProcesses: {GameId}", request.Id);
        }

        return Task.FromResult(response);
    }

    public override async Task SubscribeToGameState(Empty request, IServerStreamWriter<GameStateUpdate> responseStream, ServerCallContext context)
    {
        var peer = context.Peer;
        _logger.LogInformation("New GameState subscriber: {Peer}", peer);

        // Create an unbounded channel to buffer events
        var channel = Channel.CreateUnbounded<GameStateUpdate>();

        // Local function to handle events (Producer)
        void Handler(int gameId, GameState state)
        {
            var update = new GameStateUpdate
            {
                GameId = gameId.ToString(),
                State = state
            };

            // TryWrite is non-blocking and thread-safe
            if (!channel.Writer.TryWrite(update))
            {
                _logger.LogWarning("Failed to write GameState update to channel for {Peer}", peer);
            }
        }

        // Subscribe to events
        _sessionManager.OnGameStateChanged += Handler;

        // Send initial state for all active games
        try
        {
            var activeIds = _sessionManager.GetActiveGameIds();
            foreach (var id in activeIds)
            {
                Handler(id, GameState.Running);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending initial game states to {Peer}", peer);
        }

        try
        {
            // Consumer Loop: Read from channel and write to gRPC stream
            // This runs on the request thread, respecting async/await
            await foreach (var update in channel.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(update);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when client disconnects
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming GameState updates to {Peer}", peer);
        }
        finally
        {
            _sessionManager.OnGameStateChanged -= Handler;
            _logger.LogInformation("GameState subscriber disconnected: {Peer}", peer);
        }
    }
}

