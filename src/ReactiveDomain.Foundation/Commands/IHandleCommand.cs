namespace ReactiveDomain.Foundation.Commands
{
    public interface IHandleCommand<T> where T : Command
    {
        CommandResponse Handle(T command);
    }
}
