using Bragi.Application.Configuration;
using Bragi.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace Bragi.Infrastructure.Configuration;

public sealed class ConfigProvider : IConfigProvider
{
    private readonly BragiConfigLoader _configLoader;
    private readonly BragiConfigValidator _configValidator;
    private readonly ILogger<ConfigProvider> _logger;
    private readonly object _syncLock = new();

    private BragiConfig? _cachedConfig;

    public ConfigProvider(
        BragiConfigLoader configLoader,
        BragiConfigValidator configValidator,
        ILogger<ConfigProvider> logger)
    {
        _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
        _configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<BragiConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(LoadCore());
    }

    public BragiConfig GetRequiredConfig()
    {
        return LoadCore();
    }

    public void Validate(BragiConfig config)
    {
        _configValidator.Validate(config);
    }

    private BragiConfig LoadCore()
    {
        lock (_syncLock)
        {
            if (_cachedConfig is not null)
            {
                return _cachedConfig;
            }

            _logger.LogInformation("Loading Bragi configuration.");

            var config = _configLoader.Load();
            _configValidator.Validate(config);

            _cachedConfig = config;

            _logger.LogInformation(
                "Bragi configuration loaded and validated successfully. Category rules loaded: {CategoryRuleCount}.",
                config.CategoryRules.Count);

            return _cachedConfig;
        }
    }
}
