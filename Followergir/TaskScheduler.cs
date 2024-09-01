using Followergir.Controllers;
using Followergir.IONet;
using Library.SQL;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Followergir {

	public class TaskScheduler {

		public static void InitConfig() {
			/*TaskService ts = new TaskService();
			// Create a new task definition and assign properties
			TaskDefinition td = ts.NewTask();
			td.RegistrationInfo.Description = "Does something";

			// Create a trigger that will fire the task at this time every other day
			td.Triggers.Add(new DailyTrigger { DaysInterval = 2 });

			// Create an action that will launch Notepad whenever the trigger fires
			td.Actions.Add(new ExecAction("notepad.exe", "c:\\test.log", null));

			// Register the task in the root folder
			ts.RootFolder.RegisterTaskDefinition(@"Test", td);

			// Remove the task we just created
			ts.RootFolder.DeleteTask("Test");*/
		}



		public static async Task CheckUnFollow() {
			Database database = new Database(ServerConfig.DATABASE);
			database.DisableClose();

			int daysLength = 7;
			int deletedCount;
			do {
				database.Prepare("DELETE TOP(100) FROM [UnFollow] WHERE Date < @Date;");
				database.BindValue("@Date", DateTime.UtcNow.AddDays(-daysLength).Date, SqlDbType.Date);
				deletedCount = await database.ExecuteNonQueryAsync();
			} while (deletedCount > 0) ;


			List<Row> rows;
			for (int day = daysLength; day > 0; day--) {
				do {
					database.Prepare("SELECT TOP(100) * FROM [UnFollow] WHERE Date = @Date;");
					database.BindValue("@Date", DateTime.UtcNow.AddDays(-day).Date, SqlDbType.Date);
					rows = await database.ExecuteSelectAsync();
				} while (rows.Count > 0);
			}
			await database.CloseAsync();
		}


	}
}
