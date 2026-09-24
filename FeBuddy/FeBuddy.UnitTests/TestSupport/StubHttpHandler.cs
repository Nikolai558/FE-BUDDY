namespace FeBuddy.UnitTests.TestSupport;

/// <summary>
/// A canned <see cref="HttpMessageHandler"/> so network code can be tested without touching the
/// real internet.
/// </summary>
internal sealed class StubHttpHandler : HttpMessageHandler
{
	private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

	public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
		Task.FromResult(_responder(request));
}
