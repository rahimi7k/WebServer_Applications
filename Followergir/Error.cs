namespace Followergir.IO {

	public abstract class Error {
		//-- never use = 1 47 66 68 69 70 71 73 74 75 76  78 79 80

		public static readonly short UNKNOWN = 0;

		public static readonly short CAN_NOT_SERIALIZE = 39;
		public static readonly short CAN_NOT_DESERIALIZE = 12;
		public static readonly short FUNCTION_NOT_FOUND = 8;
		public static readonly short INVALID_PARAMETERS = 5;
		//public static readonly short TOO_MANY_REQUESTS = 25;
		//public static readonly short FLOOD_WAIT = 43;

		public static readonly short UNAUTHORIZED = 2;
		public static readonly short TOO_MANY_LOGIN = 9;
		public static readonly short LOGIN_BANNED = 53;
		public static readonly short REGESTER_BANNED = 54;
		public static readonly short HASH_EMPTY = 48;
		public static readonly short HASH_INVALID = 45;
		//public static readonly short PHONE_NUMBER_EMPTY = 59;
		public static readonly short PHONE_NUMBER_INVALID = 14;
		public static readonly short PHONE_NUMBER_BANNED = 42;
		public static readonly short EMAIL_ADDRESS_INVALID = 40;
		public static readonly short EMAIL_ADDRESS_BANNED = 51;
		public static readonly short CODE_EMPTY = 46;
		public static readonly short CODE_INVALID = 15;
		public static readonly short CODE_EXPIRED = 38;
		public static readonly short NAME_EMPTY = 61;
		public static readonly short NAME_INVALID = 17;
		public static readonly short PASSWORD_EMPTY = 44;
		public static readonly short PASSWORD_NOT_MATCH = 16;
		public static readonly short PASSWORD_LENGTH_MAX = 21;
		public static readonly short PASSWORD_LENGTH_MIM = 50;

		public static readonly short USER_ID_INVALID = 6;
		public static readonly short USER_NOT_FOUND = 11;
		public static readonly short USER_BLOCK = 4;
		public static readonly short USER_BANNED = 10;
		public static readonly short CAN_NOT_CHANGE_NAME = 31;

		//public static readonly short HASH_INVALID = 32;
		// public static readonly short NOT_JOIN_CHAT = 22;
		public static readonly short TYPE_INVALID = 56;
		// public static readonly short MESSAGE_TYPE_INVALID = 37;
		public static readonly short ACTION_NO_RESPONSE = 57;

		public static readonly short CAN_NOT_START_ORDER = 29;
		public static readonly short CAN_NOT_CANCEL_ORDER = 67;
		public static readonly short ORDER_ID_INVALID = 52;
		public static readonly short ORDER_NOT_FOUND = 23;
		public static readonly short ORDER_HAS_VIEWED = 28;
		public static readonly short ORDER_COUNT_MAX = 3;
		public static readonly short ORDER_COUNT_MIN = 7;
		public static readonly short COUNT_MIN = 27;
		public static readonly short COUNT_LIMIT = 30;
		public static readonly short CREDIT_LIMIT = 36;

		public static readonly short CAN_NOT_CHECK_PURCHASE = 24;
		public static readonly short PURCHASE_FAILED = 41;
		public static readonly short PURCHASE_HAS_USED = 26;
		public static readonly short SKU_NOT_FOUND = 72;
		public static readonly short CAN_NOT_INCREMENT_PURCHASE = 64;

		public static readonly short CAN_NOT_CONVERT = 77;
		public static readonly short TRANSFER_USER_NOT_FOUND = 33;

		public static readonly short INTERNAL_ERROR = 35;
		public static readonly short INTERNAL_DATABSE_ERROR = 34;
		//public static readonly short SERVER_ID_INVALID = 55;
		public static readonly short DB_USER_SERVER_NOT_WORK = 18;
		public static readonly short DB_USER_NOT_WORK = 19;
		public static readonly short DB_USER_T_INTERNAL_ERROR = 20;
		public static readonly short USER_ALREADY_EXIST = 60;
		public static readonly short BAD_REQUEST = 62;
		public static readonly short MAX_CONNECTION = 63;

		//public static readonly short INTERNAL_SERVER_ERROR = 13;

		//public static readonly short  = 65;
		//public static readonly short  = 58;

	}
}
