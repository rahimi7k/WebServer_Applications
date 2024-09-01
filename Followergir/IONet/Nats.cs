using NATS.Client;

namespace Followergir.IONet {

	public abstract class Nats {

		public static readonly IConnection connectionUpdate;

		static Nats() {
			ConnectionFactory cf = new ConnectionFactory();
			connectionUpdate = cf.CreateConnection("http://" + ServerConfig.IP_PROCESS + ":4222");
		}
		

	}
}

