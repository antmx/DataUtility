using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.SqlServer.Server;
//using Every1.Core.Configuration;

namespace Netricity.DataUtility.Core
{

	/// <summary>
	/// Provides SQL Server database functionality.
	/// </summary>
	public class SqlDataUtility : BaseDataUtility
	{
		#region Private Fields

		private static readonly string _defaultConnectionString;
		private static DateTime _sqlMaxDate;
		private static DateTime _sqlMinDate;

		private SqlDataAdapter _adptr;
		private bool _closeConnectionAfterFirstCommand;
		//private SqlCommand _cmd;
		private string _commandText;
		private CommandType _commandType;
		private SqlConnection _conn;
		private string _connectionString;
		//private Dictionary<string, SqlParameter> _dicOutputParams;
		//Private _reader As SqlDataReader
		//private IDataReader _reader;
		private SqlTransaction _tran;

		private bool _useTransaction;
		#endregion

		#region Properties

		/// <summary>
		/// Gets the minimum DateTime value supported by T-SQL (1753-01-01 00:00:00.000)
		/// </summary>
		public static DateTime SqlMinDate
		{
			get { return SqlDataUtility._sqlMinDate; }
		}


		/// <summary>
		/// Gets the maximum DateTime value supported by T-SQL (9999-12-31 23:59:59.997)
		/// </summary>
		/// <value>The SQL max date.</value>
		public static DateTime SqlMaxDate
		{
			get { return SqlDataUtility._sqlMaxDate; }
		}

		/// <summary>
		/// Gets the minimum DateTime2 value supported by T-SQL (0000-01-01 00:00:00.0000000)
		/// </summary>
		/// <value>The SQL min date2.</value>
		public static DateTime SqlMinDate2
		{
			get { return DateTime.MinValue; }
		}

		/// <summary>
		/// Gets the maximum DateTime2 value supported by T-SQL (9999-12-31 23:59:59.9999999)
		/// </summary>
		/// <value>The SQL max date2.</value>
		public static DateTime SqlMaxDate2
		{
			get { return DateTime.MaxValue; }
		}

		public int Timeout
		{
			get
			{
				if ((this.Command == null))
				{
					throw new CustomException("This internal Command object is null");
				}
				return this.Command.CommandTimeout;
			}
			set
			{
				if ((this.Command == null))
				{
					throw new CustomException("This internal Command object is null");
				}
				this.Command.CommandTimeout = value;
			}
		}

		//public override SqlCommand Command { get; set; }

		private SqlCommand _sqlCommand;

		public override DbCommand Command
		{
			get { return _sqlCommand; }
			//set { _sqlCommand = value; }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes the <see cref="SqlDataUtility" /> class.
		/// </summary>
		static SqlDataUtility()
		{
			//string connectionStringName = CoreSection.Settings.DataUtilConnectionStringName;
			string connectionStringName = "Connection";

			var connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];

			if (connectionStringSetting == null)
			{
				throw new CustomException("Cannot find ConnectionString '{0}'", connectionStringName);
			}

			string connectionString = connectionStringSetting.ConnectionString;
			//If flag Then
			//	connectionString = CryptoUtil.DecryptTripleDES(connectionString)
			//End If
			SqlDataUtility._defaultConnectionString = connectionString;
			SqlDataUtility._sqlMinDate = new DateTime(0x6d9, 1, 1, 0, 0, 0, 0);
			SqlDataUtility._sqlMaxDate = new DateTime(0x270f, 12, 0x1f, 0x17, 0x3b, 0x3b, 0x3e5);
		}

		public SqlDataUtility()
		{
			this._sqlCommand = new SqlCommand();

			this._commandType = CommandType.StoredProcedure;
			this._closeConnectionAfterFirstCommand = false;
			this._useTransaction = false;
		}

		public SqlDataUtility(string commandText)
			: this(commandText, CommandType.StoredProcedure, false, SqlDataUtility._defaultConnectionString)
		{
		}

		public SqlDataUtility(string commandText, bool closeConnectionAfterFirstCommand)
			: this(commandText, CommandType.StoredProcedure, closeConnectionAfterFirstCommand, SqlDataUtility._defaultConnectionString)
		{
		}

		public SqlDataUtility(string commandText, string connectionString)
			: this(commandText, CommandType.StoredProcedure, true, connectionString)
		{
		}

		public SqlDataUtility(string commandText, bool closeConnectionAfterFirstCommand, bool useTransaction)
			: this(commandText, closeConnectionAfterFirstCommand)
		{
			this.ResetTransaction();
			this.Command.CommandText = commandText;
			this._useTransaction = useTransaction;
		}

		public SqlDataUtility(string commandText, bool closeConnectionAfterFirstCommand, string connectionString)
			: this(commandText, CommandType.StoredProcedure, closeConnectionAfterFirstCommand, connectionString)
		{
		}

		public SqlDataUtility(string commandText, CommandType commandType, bool closeConnectionAfterFirstCommand)
			: this(commandText, commandType, closeConnectionAfterFirstCommand, SqlDataUtility._defaultConnectionString)
		{
		}

		public SqlDataUtility(string commandText, CommandType commandType, bool closeConnectionAfterFirstCommand, string connectionString)
			: this()
		{
			this._commandType = CommandType.StoredProcedure;
			this._closeConnectionAfterFirstCommand = false;
			this._useTransaction = false;
			this._commandText = commandText;
			this._commandType = commandType;
			this._connectionString = connectionString;
			this._closeConnectionAfterFirstCommand = closeConnectionAfterFirstCommand;
			this._conn = new SqlConnection(this._connectionString);
			this._conn.Open();
			this._sqlCommand = new SqlCommand(this._commandText, this._conn);
			this.Command.CommandType = this._commandType;
		}

		#endregion

		#region Methods

		public override void NewUpCommand(string commandText)
		{
			this._sqlCommand = new SqlCommand(commandText, this._conn);
		}

		public override DbParameter AddParamWithValue(string name, object value)
		{
			var param = _sqlCommand.Parameters.AddWithValue(name, value);

			return param;
		}

		public override void SetParamSqlDbType(string name, SqlDbType sqlDbType)
		{
			this._sqlCommand.Parameters[name].SqlDbType = sqlDbType;
		}

		public override bool ReaderHasRows()
		{
			var reader = this.Reader as SqlDataReader;

			return reader != null && reader.HasRows;
		}

		#endregion
	}
}
