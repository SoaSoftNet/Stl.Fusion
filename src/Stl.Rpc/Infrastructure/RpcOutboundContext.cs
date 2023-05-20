using Stl.Interception;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcOutboundContext
{
    private static readonly AsyncLocal<RpcOutboundContext?> CurrentLocal = new();

    public static RpcOutboundContext Current => CurrentLocal.Value ?? throw Errors.NoCurrentRpcOutboundContext();

    public static Scope NewOrActive()
    {
        var oldContext = CurrentLocal.Value;
        var context = oldContext ?? new RpcOutboundContext();
        return new Scope(context, oldContext);
    }

    public List<RpcHeader> Headers { get; set; } = new();
    public RpcMethodDef? MethodDef { get; internal set; }
    public ArgumentList? Arguments { get; internal set; }
    public CancellationToken CancellationToken { get; internal set; } = default;
    public IRpcOutboundCall? Call { get; internal set; }
    public RpcPeer? Peer { get; set; }
    public long RelatedCallId { get; set; }

    public Scope Activate()
        => new(this);

    public Task SendCall(RpcMethodDef methodDef, ArgumentList arguments)
    {
        if (Call != null)
            throw Stl.Internal.Errors.AlreadyInvoked(nameof(SendCall));

        // MethodDef, Arguments, CancellationToken
        MethodDef = methodDef;
        Arguments = arguments;
        var ctIndex = methodDef.CancellationTokenIndex;
        CancellationToken = ctIndex >= 0 ? arguments.GetCancellationToken(ctIndex) : default;

        // Peer
        Peer ??= MethodDef.Hub.PeerResolver.Invoke(this);
        if (Peer == null)
            return Task.CompletedTask;

        // Call
        var call = Call = methodDef.CallFactory.CreateOutbound(this);
        return call.Send().AsTask();
    }

    // Nested types

    public readonly struct Scope : IDisposable
    {
        private readonly RpcOutboundContext? _oldContext;

        public readonly RpcOutboundContext Context;

        internal Scope(RpcOutboundContext context)
        {
            Context = context;
            _oldContext = CurrentLocal.Value;
            TryActivate(context);
        }

        internal Scope(RpcOutboundContext context, RpcOutboundContext? oldContext)
        {
            Context = context;
            _oldContext = oldContext;
            TryActivate(context);
        }

        public void Dispose()
            => TryActivate(_oldContext);

        private void TryActivate(RpcOutboundContext? context)
        {
            if (Context != _oldContext)
                CurrentLocal.Value = context;
        }
    }
}
