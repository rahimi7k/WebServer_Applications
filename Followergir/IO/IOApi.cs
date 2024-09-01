using Newtonsoft.Json;
using System.Collections.Generic;

namespace Followergir.IO {

	public abstract class IOApi {

		public class Initial : IOObject {
			public string os;
			public string language;
			public int version;
			public bool enableAuthentication;
		}

		public abstract class Function : IOObject {

		}

		public class SetAuthenticationPhoneNumber : Function {
			public string phoneNumber;
		}

		public class SetAuthenticationEmailAddress : Function {
			public string emailAddress;
		}


		public class CheckAuthenticationCode : Function {
			public string hash;
			public string code;
		}

		public class CheckAuthenticationPassword : Function {
			public string hash;
			public string password;
		}

		//authRecoveryPassword
		//checkRecoveryPassword

		public class SetAuthenticationRegister : Function {
			public string hash;
			public string code;
			public string name;
			public string password;
		}

		public class ResendAuthenticationCode : Function {
			public string hash;
			public byte state;
		}

		public class SetAuthenticationForgotPassword : Function {
			public string hash;
		}


		public class SetAuthenticationChangePassword : Function {
			public string hash;
			public string code;
			public string password;
		}

		public class CancelAuthentication : Function {
			public string hash;
		}

		public class GetMe : Function {
			public long userId;
			public string session;
		}

		public class ChangeName : Function {
			public string name;
		}

		public class ChangePhoneNumber : Function {
			public string phoneNumber;
		}

		public class ChangeEmailAddress : Function {
			public string emailAddress;
		}

		public class ChangePassword : Function {
			public string oldPassword;
			public string newPassword;
		}

		public class LogOut : Function {

		}

		public class DeleteAccount : Function {

		}



		public class AddUser : Function {
			public long iId;
			public Order order;
			public string hash;
			public string data;
		}

		public class AddLike : Function {
			public long iId;
			public Order order;
			public int type;
			public string hash;
			public string data;
		}

		public class AddComment : Function {
			public long iId;
			public Order order;
			public string comment;
			public int type;
			public string hash;
			public string data;
		}

		public class GetListOrder : Function {
			public List<ListOrderItem> list;
		}


		public class GetOrderInfo : Function {
			public int index;
		}

		public class GetOrderHistory : Function {
			public long id;
			public bool isNew;
		}

		public class OrderUser : Function {
			public Order order;
			public int count;
			public int initialCount;
		}

		public class OrderLike : Function {
			public Order order;
			public int count;
			public int initialCount;
			public int type;
		}

		public class OrderComment : Function {
			public Order order;
			public List<string> comments;
			public int initialCount;
			public int type;
		}


		public class OrderStart : Function {
			public Order order;
			public byte type;
		}

		public class OrderCancel : Function {
			public Order order;
			public byte type;
		}

		public class OrderError : Function {
			public long iId;
			public Order order;
			public byte type;
			public string error;
			public string hash;
			public string data;
		}

		public class GetStorePackages : Function {

		}


		public class CheckBuyWeb : Function {
			public string id;
		}


		public class CheckBuyAndroid : Function {
			public byte market;
			public string app;
			public string sku;
			public string token;
		}



		public class CheckUnFollow : Function {
			public long iId;
			public int index;
			public int count;
		}


		public class SetData : Function {
			public long id;
			public string data;
		}



		//API
		public class ApiGetSession : Function {	
		}

		public class ApiCreateOrUpdateSession : Function {
		}

		public class ApiUpdateActivation : Function {
		}






		public class Order : IOObject {
			public string username;
			public string postId;
			public long userId;
		}





		public class Ok : IOObject {

		}

		public class Error : IOObject {
			//public short code;
			public string message;

			public Error() {

			}

			/*public Error(short code) {
				this.code = code;
			}*/

			public Error(string message) {
				this.message = message;
			}

			/*public Error(short code, string message) {
				this.code = code;
				this.message = message;
			}

			public override string ToString() {
				if (message == null || message == "") {
					return "Error: 0x" + code.ToString("X4");
				}
				return "Error: " + message + " (0x" + code.ToString("X4") + ")";
			}*/
		}

		public abstract class AuthorizationState : IOObject {

		}

		public class AuthorizationStateWaitPhoneNumber : AuthorizationState {
		
		}

		public class AuthorizationStateWaitCode : AuthorizationState {
			public string hash;
			public int length;
			public int timeout;
		}

		public class AuthorizationStateWaitPassword : AuthorizationState {
			public string hash;
			//public bool hasRecoveryEmailAddress;
			//public string recoveryEmailPattern;
		}


		public class AuthorizationStateReady : AuthorizationState {
			public long userId;
			public string session;
		}

		public class SyncApp : IOObject {
			public UserFull user;
			public List<OfferItem> listOffer;
			public List<Message> listMessage;
			public Config config;
		}

		public class User : IOObject {
			public string name;
			public string phoneNumber;
			public string emailAddress;
		}

		public class UserFull : User {
			public double coin;
		}

		public class ListOrder : IOObject {
			public List<ListOrderItem> list;
		}


		[JsonConverter(typeof(SerializedData.AbstractConverter))]
		public abstract class ListOrderItem : IOObject {
			public long iId;
			public Order order;
			public string hash;
			public bool needData;
		}

		public class ListOrderUser : ListOrderItem {
			public new string T = "ListOrderUser";
		}

		public class ListOrderLike : ListOrderItem {
			public new string T = "ListOrderLike";
		}

		public class ListOrderComment : ListOrderItem {
			public new string T = "ListOrderComment";
			public string text;
		}

		public class OrderInfo : IOObject {
			public List<OrderInfoItem> list;
			public bool hasMore;
		}


		public class OrderHistory : IOObject {
			public List<OrderHistoryItem> list;
			public bool hasMore;
		}


		public class UnFollow : IOObject {
			public string list;
			public bool hasMore;
		}

		public class UnCoin : IOObject {
			public int count;
			public double coin;
		}

		public class StorePackages : IOObject {
			public List<StorePackageItem> list;
		}


		public class StorePackageItem : IOObject {
			public string id;
			public double coin;
			public double price;
		}

		public class OfferItem : IOObject {
			public string id;
			public double coin;
			public int dollar;
			public byte icon;
		}

		public class Message : IOObject {
			public string id;
			public string text;
		}

		public class OrderInfoItem : IOObject {
			public Order order;
			public byte type;
			public int count;
			public int remaining;
			public string error;
			public int status;
			public long lastCartId;
		}



		public class OrderHistoryItem : IOObject {
			public long id;
			public Order order;
			public byte type;
			public int count;
			public long date;
		}



		public class Config : IOObject {
			public RateConfig rate;
			public AddConfig add;
			public OrderConfig order;
			public OrderInfoConfig orderInfo;
		}

		public class RateConfig : IOObject {
			public float user;
			public float like;
			public float comment;
		}

		public class AddConfig : IOObject {
			public byte lengthList;
		}

		public class OrderConfig : IOObject {
			public int minUser;
			public int maxUser;
			public int minLike;
			public int maxLike;
			public int minComment;
			public int maxComment;
		}

		public class OrderInfoConfig : IOObject {
			public double cancelRate;
		}





		//API
		public class ApiSession : IOObject {
			public string session;
			public bool isActive;
		}











		/// <summary>
		/// 
		/// </summary>
		public abstract class Update : IOObject {
			public long unixTime;
		}

		public class UpdateName : Update {
			public string name;
		}

		public class UpdatePhoneNumber : Update {
			public string phoneNumber;
		}

		public class UpdateEmailAddress : Update {
			public string emailAddress;
		}

		public class UpdateSession : Update {
			public string session;
		}

		public class UpdateUser : Update {
			public User user;
		}

		public class UpdateCoin : Update {
			public double coin;
		}

		public class UpdateOrderInfo : Update {
			public OrderInfoItem orderInfo;
		}

		public class UpdateOrderHistory : Update {
			public OrderHistoryItem orderHistory;
			public long lastCartId;
		}

		public class UpdateStorePackages : Update {
			public StorePackageItem storePackage;
		}

		public class UpdateOffer : Update {
			public List<OfferItem> list;
		}

		public class UpdateUnFollow : Update {
			public double count;
		}

		public class UpdateUnCoin : Update {
			public double coin;
		}

		public class UpdateMessage : Update {
			public Message message;
		}

		public class UpdateNewMessage : Update {
			public Message message;
		}

		public class UpdateDeleteMessage : Update {
			public int id;
		}

		public class UpdateEncryption : Update {
			public string key;
			public string iv;
		}

		public class UpdateRateConfig : Update {
			public RateConfig config;
		}

		public class UpdateAddConfig : Update {
			public AddConfig config;
		}

		public class UpdateOrderConfig : Update {
			public OrderConfig config;
		}

		public class UpdateOrderInfoConfig : Update {
			public OrderInfoConfig config;
		}








		public abstract class Action : IOObject {

		}

		public class ActionGetAuthentication : Action {
			public long iId;
		}

		public class ActionGetAuthenticationAll : Action {
			public long iId;
		}

		public class ActionCheckFollow : Action {
			public long iId;
			public string list;
		}

		public class ActionCheckUnFollow : Action {
			public long iId;
			public string orderId;
		}




		public abstract class ActionResult : IOObject {

		}


		public class AuthenticationAAA : ActionResult {

		}

		public class AuthenticationAll : ActionResult {

		}

		public class ActionUnFollowResponse : ActionResult {
			public string hash;
			public bool res;
		}

		public class FollowClientResult : ActionResult {
			public long iId;
			public bool isFollow;
		}
	}
}
