namespace Followergir.IO {

	public sealed class Message {

		public int identifierFunction;
		public IOObject ioFunction;

		public IOApi.Update[] updates;

		public string identifierAction;
		public IOObject ioAction;

	}
}
