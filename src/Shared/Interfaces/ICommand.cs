namespace ModularMonolith.Shared.Interfaces;

/// <summary>
/// Marker interface for commands (write operations)
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Command with return value
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommand<out TResponse> : ICommand
{
}