using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Library.SQL {

	public class Database {

		private SqlConnection sqlConnection;
		private SqlCommand sqlCommand;

		private bool closeDatabase = true;

		/*public Database() {
			
		}*/

		public Database(string connectionString) : this(new SqlConnection(connectionString)) {

		}

		public Database(SqlConnection sqlConnection) {
			this.sqlConnection = sqlConnection;
		}

		public Database(string database, string username, string password) : this(Environment.MachineName, database, username, password) {
		}

		public Database(string server, string database, string username, string password) {
			sqlConnection = new SqlConnection("Server=" + server + ";Database=" + database + ";User Id=" + username + ";Password=" + password + ";");
		}

		public void SetConnection(string connectionString) {
			if (sqlCommand != null) {
				sqlCommand.Dispose();
				sqlCommand = null;
			}
			Close();
			sqlConnection = new SqlConnection(connectionString);
		}

		public void Prepare(string query) {
			if (sqlCommand != null) {
				sqlCommand.Dispose();
				sqlCommand = null;
			}
			sqlCommand = new SqlCommand(query, sqlConnection);
			if (!IsOpen()) {
				sqlConnection.Open();
			}
		}

		/*public void BindValue(string name, object value) {
			sqlCommand.Parameters.AddWithValue(name, value);
		}*/

		public void BindValue(string name, object value, SqlDbType type) {
			sqlCommand.Parameters.Add(name, type).Value = value;
		}

		public void BindValue(string name, object value, SqlDbType type, int size) {
			sqlCommand.Parameters.Add(name, type, size).Value = value;
		}

		public List<Row> ExecuteSelect() {
			SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
			List<Row> rows = new List<Row>();
			if (sqlDataReader.HasRows) {
				while (sqlDataReader.Read()) {
					Row row = new Row();
					for (int i = 0; i < sqlDataReader.FieldCount; i++) {
						row.Put(sqlDataReader.GetName(i), sqlDataReader.GetValue(i));
					}
					rows.Add(row);
				}
			}
			sqlDataReader.Dispose();
			sqlDataReader.Close();
			sqlCommand.Dispose();
			sqlCommand = null;
			if (closeDatabase) {
				Close();
			}
			return rows;
		}

		public async Task<List<Row>> ExecuteSelectAsync() {
			SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();
			List<Row> rows = new List<Row>();
			if (sqlDataReader.HasRows) {
				while (sqlDataReader.Read()) {
					Row row = new Row();
					for (int i = 0; i < sqlDataReader.FieldCount; i++) {
						row.Put(sqlDataReader.GetName(i), sqlDataReader.GetValue(i));
					}
					rows.Add(row);
				}
			}
			await sqlDataReader.DisposeAsync();
			await sqlDataReader.CloseAsync();
			await sqlCommand.DisposeAsync();
			sqlCommand = null;
			if (closeDatabase) {
				Close();
			}
			return rows;
		}

		public object ExecuteInsert() {
			object obj = sqlCommand.ExecuteScalar();
			sqlCommand.Dispose();
			sqlCommand = null;
			if (closeDatabase) {
				Close();
			}
			return obj;
		}

		public async Task<object> ExecuteInsertAsync() {
			object obj = await sqlCommand.ExecuteScalarAsync();
			await sqlCommand.DisposeAsync();
			sqlCommand = null;
			if (closeDatabase) {
				Close();
			}
			return obj;
		}

		public object ExecuteUpdate() {
			object obj = sqlCommand.ExecuteScalar();
			sqlCommand.Dispose();
			sqlCommand = null;
			if (closeDatabase) {
				Close();
			}
			return obj;
		}

		public async Task<object> ExecuteUpdateAsync() {
			object obj = await sqlCommand.ExecuteScalarAsync();
			await sqlCommand.DisposeAsync();
			sqlCommand = null;
			if (closeDatabase) {
				Close();
			}
			return obj;
		}


		public object ExecuteDelete() {
			return ExecuteUpdate();
		}

		public Task<object> ExecuteDeleteAsync() {
			return ExecuteUpdateAsync();
		}

		/// <summary>
		/// Executes a Transact-SQL statement against the connection and returns the number of rows affected.
		/// </summary>
		/// <returns>The number of rows affected.</returns>
		public int ExecuteNonQuery() {
			int count = sqlCommand.ExecuteNonQuery();
			sqlCommand.Dispose();
			sqlCommand = null;
			if (closeDatabase) {
				Close();
			}
			return count;
		}

		/// <summary>
		/// Executes a Transact-SQL statement against the connection and returns the number of rows affected.
		/// </summary>
		/// <returns>The number of rows affected.</returns>
		public async Task<int> ExecuteNonQueryAsync() {
			int count = await sqlCommand.ExecuteNonQueryAsync();
			await sqlCommand.DisposeAsync();
			sqlCommand = null;
			if (closeDatabase) {
				Close();
			}
			return count;
		}

		public bool IsOpen() {
			return sqlConnection != null && sqlConnection.State == ConnectionState.Open;
		}

		public void Close() {
			if (IsOpen()) {
				sqlConnection.Close();
				sqlConnection = null;
			}
			/*if (sqlCommand != null) {
				sqlCommand.Dispose();
				sqlCommand = null;
			}*/
		}

		public async Task CloseAsync() {
			if (IsOpen()) {
				await sqlConnection.CloseAsync();
				sqlConnection = null;
			}
			/*if (sqlCommand != null) {
				sqlCommand.DisposeAsync();
				sqlCommand = null;
			}*/
		}

		public void DisableClose() {
			closeDatabase = false;
		}
	}
}
