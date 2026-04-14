using Bragi.Application.Configuration;

namespace Bragi.Application.Contracts;

public interface IConfigProvider
{
    Task<BragiConfig> LoadAsync(CancellationToken cancellationToken = default);

    BragiConfig GetRequiredConfig();

    void Validate(BragiConfig config);
}
