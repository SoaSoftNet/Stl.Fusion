using Stl.Interception;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public abstract class RpcArgumentSerializer
{
    public static RpcArgumentSerializer Default { get; set; } = new RpcByteArgumentSerializer(ByteSerializer.Default);

    public abstract RpcMessage CreateMessage(long callId, RpcMethodDef methodDef, ArgumentList arguments, List<RpcHeader> headers);
    public abstract ArgumentList Deserialize(RpcMessage message, Type argumentListType);
}

public abstract class RpcArgumentSerializer<TArgumentData> : RpcArgumentSerializer
{
    public override RpcMessage CreateMessage(long callId, RpcMethodDef methodDef, ArgumentList arguments, List<RpcHeader> headers)
    {
        var serializedArguments = Serialize(arguments, arguments.GetType());
        return new RpcMessage<TArgumentData>(callId, methodDef.Service.Name, methodDef.Name, serializedArguments, headers);
    }

    public override ArgumentList Deserialize(RpcMessage message, Type argumentListType)
    {
        var typedMessage = (RpcMessage<TArgumentData>)message;
        return Deserialize(typedMessage.ArgumentData, argumentListType);
    }

    // Protected methods

    protected abstract TArgumentData Serialize(ArgumentList arguments, Type argumentListType);
    protected abstract ArgumentList Deserialize(TArgumentData data, Type argumentListType);
}
