namespace Remote.Viewer.Identity;

public sealed class ViewerIdentityProvider
{

    private readonly Guid _viewerId = Guid.NewGuid();

    public Guid Get()
    {
        return _viewerId;
    }

}