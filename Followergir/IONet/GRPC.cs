using Grpc.Net.Client;
using System;
using System.Net.Http;

namespace Followergir.IONet {

	public class GRPC {

		private static gRPC_Member.MemberService.MemberServiceClient clientMember;
		private static gRPC_Followergir.FollowergirService.FollowergirServiceClient clientFollowergir;

		static GRPC() {
			if (App.IsDebug()) {
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			}
		}

		public static gRPC_Member.MemberService.MemberServiceClient GetMember() {
			if (clientMember == null) {
				GrpcChannelOptions grpcChannelOptions = new GrpcChannelOptions();
				grpcChannelOptions.HttpHandler = new SocketsHttpHandler {
					EnableMultipleHttp2Connections = true,
					MaxConnectionsPerServer = 100,
					ConnectTimeout = TimeSpan.FromSeconds(10)
				};
				GrpcChannel channel = GrpcChannel.ForAddress("http://" + ServerConfig.IP_DATABASE + ":" + ServerConfig.PORT_GRPC_MEMBER, grpcChannelOptions);
				clientMember = new gRPC_Member.MemberService.MemberServiceClient(channel);
			}
			return clientMember;
		}

		public static gRPC_Followergir.FollowergirService.FollowergirServiceClient GetFollowergir() {
			if (clientFollowergir == null) {
				GrpcChannelOptions grpcChannelOptions = new GrpcChannelOptions();
				grpcChannelOptions.HttpHandler = new SocketsHttpHandler {
					EnableMultipleHttp2Connections = true,
					MaxConnectionsPerServer = 100,
					ConnectTimeout = TimeSpan.FromSeconds(20)
				};
				GrpcChannel channel = GrpcChannel.ForAddress("http://" + ServerConfig.IP_DATABASE + ":" + ServerConfig.PORT_GRPC_FOLLOWERGIR, grpcChannelOptions);
				clientFollowergir = new gRPC_Followergir.FollowergirService.FollowergirServiceClient(channel);
			}
			return clientFollowergir;
		}



	}
}
