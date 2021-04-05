using System;
using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Grpc.Instrumentation
{
    internal static class GrpcInstrumentation
    {
        private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.CecoChatGrpc";
        private static readonly AssemblyName _assemblyName = typeof(GrpcInstrumentation).Assembly.GetName();
        private static readonly Version _activitySourceVersion = _assemblyName.Version;

        internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, _activitySourceVersion.ToString());

        public static class Keys
        {
            public const string TagRpcSystem = "rpc.system";
            public const string TagRpcService = "rpc.service";
            public const string TagRpcMethod = "rpc.method";
        }

        public static class Values
        {
            public const string TagRpcSystemGrpc = "grpc";
        }
    }
}
