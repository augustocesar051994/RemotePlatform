namespace Remote.Agent.Identity;

public sealed class AgentIdentityProvider
{
    private readonly string _identityFilePath;

    public AgentIdentityProvider()
    {
        var applicationDataPath = Environment.GetFolderPath(
            Environment.SpecialFolder.CommonApplicationData
        );

        var directoryPath = Path.Combine(
            applicationDataPath,
            "RemotePlatform"
        );

        Directory.CreateDirectory(directoryPath);

        _identityFilePath = Path.Combine(
            directoryPath,
            "agent.id"
        );
    }

    public Guid GetOrCreate()
    {
        if (File.Exists(_identityFilePath))
        {
            var content = File.ReadAllText(_identityFilePath).Trim();

            if (Guid.TryParse(content, out var existingAgentId))
            {
                return existingAgentId;
            }
        }

        var newAgentId = Guid.NewGuid();

        File.WriteAllText(
            _identityFilePath,
            newAgentId.ToString()
        );

        return newAgentId;
    }
}