// Assembly 'Microsoft.Extensions.AsyncState'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AsyncState;

/// <summary>
/// Encapsulates all information within the asynchronous flow in an <see cref="T:System.Threading.AsyncLocal`1" /> variable.
/// </summary>
public interface IAsyncState
{
    /// <summary>
    /// Initializes async state in current asynchronous flow.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Resets async state after usage.
    /// </summary>
    void Reset();

    /// <summary>
    /// Tries to get the stored async context from the state.
    /// </summary>
    /// <param name="token">The token representing the state to extract.</param>
    /// <param name="value">
    /// Receives the value associated with the specified token, if the context is initialized;
    /// otherwise, the default value for the type of the <paramref name="value" /> parameter.
    /// </param>
    /// <returns><see langword="true" /> if the context is initialized; otherwise, <see langword="false" />.</returns>
    bool TryGet(AsyncStateToken token, [MaybeNullWhen(false)] out object? value);

    /// <summary>
    /// Gets the stored async context from the state.
    /// </summary>
    /// <param name="token">The token representing the state to extract.</param>
    /// <exception cref="T:System.InvalidOperationException">If the context is not initialized.</exception>
    /// <returns>The asynchronous state corresponding to the token.</returns>
    object? Get(AsyncStateToken token);

    /// <summary>
    /// Stores async context.
    /// </summary>
    /// <param name="token">The token representing the state to store.</param>
    /// <param name="value">New state value.</param>
    /// <exception cref="T:System.InvalidOperationException">Context is not initialized.</exception>
    void Set(AsyncStateToken token, object? value);

    /// <summary>
    /// Registers new async context with the state.
    /// </summary>
    /// <returns>Token that gives access to the reserved context.</returns>
    AsyncStateToken RegisterAsyncContext();
}
