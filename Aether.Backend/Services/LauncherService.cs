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
                RunAsAdmin = request.RunAsAdmin
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
                // Update last played timestamp
                game.LastPlayed = DateTime.UtcNow;
                game.UpdatedAt = DateTime.UtcNow;
                _database.UpsertGame(game);

                _logger.LogInformation("Successfully launched {Title} via {Method}", game.Title, result.LaunchMethod);

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
        // TODO: Implement game stopping logic (platform-specific)
        return Task.FromResult(new OperationStatus { Success = true, Message = "Stopped" });
    }

    public override async Task SubscribeToGameState(Empty request, IServerStreamWriter<GameStateUpdate> responseStream, ServerCallContext context)
    {
        // TODO: Implement game state monitoring
        await Task.Delay(100);
    }
}

