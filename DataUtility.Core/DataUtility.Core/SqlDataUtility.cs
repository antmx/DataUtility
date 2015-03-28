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
		private IDataReader _reader;
		private SqlTransaction _tran;

		private bool _useTransaction;
		#endregion

		#region Properties

		public IDataReader Reader
		{
			get
			{
				if ((this._reader == null))
				{
					throw new ApplicationException("Internal DataReader is not initialised.");
				}
				return this._reader;
			}
		}

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
				if ((this._cmd == null))
				{
					throw new CustomException("This internal Command object is null");
				}
				return this._cmd.CommandTimeout;
			}
			set
			{
				if ((this._cmd == null))
				{
					throw new CustomException("This internal Command object is null");
				}
				this._cmd.CommandTimeout = value;
			}
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
			this._cmd.CommandText = commandText;
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
			this._cmd = new SqlCommand(this._commandText, this._conn);
			this._cmd.CommandType = this._commandType;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Adds an output parameter to the command.
		/// The output result can be retrieved via <see cref="GetOutputParamValue" /> after the command has been executed.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		public override void AddOutputParam(string name, object value)
		{
			if ((this._dicOutputParams == null))
			{
				this._dicOutputParams = new Dictionary<string, DbParameter>();
			}
			if (!this._dicOutputParams.ContainsKey(name))
			{
				SqlParameter parameter = new SqlParameter(name, value) { Direction = ParameterDirection.InputOutput };
				this._cmd.Parameters.Add(parameter);
				this._dicOutputParams.Add(name, parameter);
			}
			_cmd.Parameters.AddWithValue(name, value).Direction = ParameterDirection.InputOutput;
		}

		public override void AddParam(string name, IEnumerable value)
		{
			if (((value == null) || value is string))
			{
				this._cmd.Parameters.AddWithValue(name, value);
			}
			else
			{
				SqlXml xml = null;
				MemoryStream output = new MemoryStream();
				using (XmlWriter.Create(output))
				{
					XmlSerializer serializer = new XmlSerializer(value.GetType());
					XmlTextWriter writer2 = new XmlTextWriter(output, System.Text.Encoding.ASCII);
					UTF8Encoding encoding = new UTF8Encoding();
					output = (MemoryStream)writer2.BaseStream;
					serializer.Serialize((Stream)output, value);
					xml = new SqlXml(output);
					this._cmd.Parameters.AddWithValue(name, xml.Value);

					//var idx = this._cmd.Parameters.Add(xml.Value);
					//var param = this._cmd.Parameters[idx];
					//param.ParameterName = name;
					//param.di

					writer2 = null;
					serializer = null;
				}
			}
		}

		public void AddParam(string name, object value)
		{
			this._cmd.Parameters.AddWithValue(name, value);
		}

		public void AddParam(string name, string value, int maxLen)
		{
			if ((((value != null)) && (value.Length > maxLen)))
			{
				value = value.Substring(0, maxLen);
			}
			this._cmd.Parameters.AddWithValue(name, value);
		}

		/// <summary>
		/// Adds a table value parameter to the current command.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		public void AddTableValueParam(string name, IEnumerable<SqlDataRecord> value)
		{
			this._cmd.Parameters.AddWithValue(name, value).SqlDbType = SqlDbType.Structured;
		}

		private void EnsureFieldExists(string fieldName)
		{
			if (((this._reader[fieldName] == null) || this._reader.IsDBNull(this._reader.GetOrdinal(fieldName))))
			{
				throw new CustomException("{0} cannot find a field called '{1}' containing non-null data from '{2}'", new object[] {
					base.GetType(),
					fieldName,
					this._cmd.CommandText
				});
			}
		}

		private int EnsureFieldExistsOrIsNull(string fieldName)
		{
			if ((this._reader[fieldName] == null))
			{
				throw new CustomException("{0} cannot find a field called '{1}' from '{2}'.", new object[] {
					base.GetType(),
					fieldName,
					this._cmd.CommandText
				});
			}
			return this._reader.GetOrdinal(fieldName);
		}

		/// <summary>
		/// Checks if a field exists exists in the current row of the DataReader.
		/// Returns true if so; false otherwise.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		public bool CheckFieldExists(string fieldName)
		{

			if (this._reader == null)
			{
				return false;
			}

			for (int i = 0; i <= this._reader.FieldCount - 1; i++)
			{
				//if (StringUtil.EqualsIgnoreCase(this._reader.GetName(i), fieldName))
				if (String.Equals(this._reader.GetName(i), fieldName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;

		}

		public bool CommitTransaction()
		{
			try
			{
				if (((this._tran != null)))
				{
					this._tran.Commit();
				}
			}
			catch (Exception)
			{
				this._tran.Rollback();
				return false;
			}
			return true;
		}

		public DataSet ExecuteDataSet()
		{
			this._adptr = new SqlDataAdapter();
			this._adptr.SelectCommand = this._cmd;
			DataSet dataSet = new DataSet();
			this._adptr.Fill(dataSet);
			return dataSet;
		}

		public DataTable ExecuteDataTable()
		{
			DataSet @set = this.ExecuteDataSet();
			if ((@set.Tables.Count > 0))
			{
				return @set.Tables[0];
			}
			return null;
		}

		public int ExecuteNonQuery()
		{
			return this._cmd.ExecuteNonQuery();
		}

		public int ExecuteNonQuery(int expectedRowsAffected)
		{
			int num = this.ExecuteNonQuery();
			if ((num != expectedRowsAffected))
			{
				throw new CustomException("{0} affected {1} rows; {2} expected.", this._cmd.CommandText, num, expectedRowsAffected);
			}
			return num;
		}

		public void ExecuteReader()
		{
			if (this._closeConnectionAfterFirstCommand)
			{
				this._reader = this._cmd.ExecuteReader(CommandBehavior.CloseConnection);
			}
			else
			{
				this._reader = this._cmd.ExecuteReader();
			}
		}

		/// <summary>
		/// Executes the command, and returns the value of the first column of the first row in the result set, cast to the given type.
		/// Additional columns or rows are ignored.
		/// </summary>
		/// <typeparam name="T">The type of the data in the first column of the first row of the result set.</typeparam>
		/// <returns></returns>
		public T ExecuteScalar<T>()
		{
			object obj2 = this._cmd.ExecuteScalar();
			if (obj2 is T)
			{
				return (T)obj2;
			}
			//return (T)null;
			return default(T);
		}

		public XmlReader ExecuteXmlReader()
		{
			return this._cmd.ExecuteXmlReader();
		}

		public bool GetBool(string fieldName)
		{
			this.EnsureFieldExists(fieldName);
			return ((this._reader[fieldName].ToString() == "1") || (this._reader[fieldName].ToString().ToLower() == "true"));
		}

		public bool GetBool(string fieldName, string trueString)
		{
			this.EnsureFieldExists(fieldName);
			return this.GetBool(fieldName, trueString, true);
		}

		public bool GetBool(string fieldName, string trueString, bool ignoreCase)
		{
			this.EnsureFieldExists(fieldName);
			return (string.Compare(this._reader[fieldName].ToString(), trueString, ignoreCase) == 0);
		}

		public byte[] GetByteArray(string fieldName)
		{
			this.EnsureFieldExistsOrIsNull(fieldName);
			return (byte[])this._reader[fieldName];
		}

		public DateTime GetDateTime(string fieldName)
		{
			this.EnsureFieldExists(fieldName);
			return Convert.ToDateTime(this._reader[fieldName]);
		}

		public double GetDouble(string fieldName)
		{
			this.EnsureFieldExists(fieldName);
			return double.Parse(this._reader[fieldName].ToString());
		}

		public T GetEnum<T>(string fieldName)
		{
			dynamic requestedType = typeof(T);
			if (!requestedType.IsEnum)
			{
				throw new CustomException("T must be an Enum type");
			}
			this.EnsureFieldExistsOrIsNull(fieldName);
			return (T)Enum.Parse(requestedType, this._reader[fieldName].ToString(), true);
		}

		public T GetFromXml<T>(string fieldName)
		{
			int ordinal = this.EnsureFieldExistsOrIsNull(fieldName);
			if (this._reader.IsDBNull(ordinal))
			{
				//return (T)null;
				return default(T);
			}
			string s = this._reader[fieldName].ToString();
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			MemoryStream input = new MemoryStream(new ASCIIEncoding().GetBytes(s));
			XmlReader xmlReader = XmlReader.Create(input);
			return (T)serializer.Deserialize(xmlReader);
		}

		public int GetInt(string fieldName)
		{
			int num = 0;
			this.EnsureFieldExists(fieldName);
			try
			{
				num = int.Parse(this._reader[fieldName].ToString());
			}
			catch (Exception)
			{
				throw new CustomException(string.Format("{0} = {1}", fieldName, this._reader[fieldName]));
			}
			return num;
		}

		public Nullable<DateTime> GetNullableDateTime(string fieldName)
		{
			int ordinal = this.EnsureFieldExistsOrIsNull(fieldName);
			if (this._reader.IsDBNull(ordinal))
			{
				return null;
			}
			return new Nullable<DateTime>(Convert.ToDateTime(this._reader[fieldName]));
		}

		public Nullable<int> GetNullableInt(string fieldName)
		{
			int ordinal = this.EnsureFieldExistsOrIsNull(fieldName);
			if (this._reader.IsDBNull(ordinal))
			{
				return null;
			}
			return new Nullable<int>(int.Parse(this._reader[fieldName].ToString()));
		}

		public string GetNullableString(string fieldName)
		{
			int ordinal = this.EnsureFieldExistsOrIsNull(fieldName);
			if (this._reader.IsDBNull(ordinal))
			{
				return null;
			}
			return this._reader[fieldName].ToString();
		}

		//public T GetOutputParamValue<T>(string paramName)
		//{
		//	SqlParameter parameter = this._dicOutputParams[paramName];
		//	return (T)parameter.Value;
		//}

		public string GetString(string fieldName)
		{
			this.EnsureFieldExists(fieldName);
			return this._reader[fieldName].ToString();
		}

		public T GetValue<T>(string fieldName, T defaultValue = default(T))
		{

			dynamic requestedType = typeof(T);
			dynamic underlyingType = Nullable.GetUnderlyingType(requestedType);
			int ordinal = 0;
			//T objValue = null;
			T objValue = default(T);

			if (underlyingType != null)
			{
				// Get the ordinal of the nullable field
				ordinal = EnsureFieldExistsOrIsNull(fieldName);

				if (this._reader.IsDBNull(ordinal))
				{
					return defaultValue;
				}

				//objValue = DirectCast(Convert.ChangeType(Me._reader(ordinal), underlyingType), T)

				if (underlyingType.IsEnum)
				{
					// Need to handle enums differently
					if (!Enum.IsDefined(underlyingType, this._reader[ordinal]))
					{
						throw new CustomException("Cannot convert field {0}'s value '{1}' to {2}", fieldName, this._reader[ordinal], underlyingType);
					}
					else
					{
						objValue = (T)Enum.ToObject(underlyingType, this._reader[ordinal]);
					}
				}
				else
				{
					objValue = (T)Convert.ChangeType(this._reader[ordinal], underlyingType);
				}

			}
			else
			{
				ordinal = this._reader.GetOrdinal(fieldName);

				if (this._reader.IsDBNull(ordinal))
				{
					return defaultValue;
				}

				if (requestedType.IsEnum)
				{
					// Need to handle enums differently
					if (!Enum.IsDefined(requestedType, this._reader[ordinal]))
					{
						throw new CustomException("Cannot convert field {0}'s value '{1}' to {2}", fieldName, this._reader[ordinal], requestedType);
					}
					else
					{
						objValue = (T)Enum.ToObject(requestedType, this._reader[ordinal]);
					}
				}
				else
				{
					objValue = (T)Convert.ChangeType(this._reader[ordinal], requestedType);
				}
			}

			return objValue;

		}

		/// <summary>
		/// Determines whether the given generic type can store a null value.
		/// Returns True if the type is a reference type or a nullable value type. 
		/// Returns false if the type a non-nullable value type.
		/// </summary>
		/// <typeparam name="T">The type to test.</typeparam>
		private bool IsNullableType<T>()
		{

			dynamic objType = typeof(T);

			dynamic isNullable = IsNullableType(objType);

			return isNullable;

		}

		private bool IsNullableType(Type objType)
		{

			if (objType == null)
			{
				throw new ArgumentNullException("objType");
			}

			if (!objType.IsValueType)
			{
				return true;
				// ref-type
			}

			if (Nullable.GetUnderlyingType(objType) != null)
			{
				return true;
				// Nullable(Of T)
			}

			return false;
			// value-type

		}

		public bool NextResult()
		{
			if ((this._reader == null))
			{
				return false;
			}
			return this._reader.NextResult();
		}

		public bool NextRow()
		{
			if ((this._reader == null))
			{
				return false;
			}
			//If Not Me._reader.HasRows Then
			//	Return False
			//End If
			dynamic sqlReader = this._reader as SqlDataReader;
			if (!sqlReader.HasRows)
			{
				return false;
			}
			return this._reader.Read();
		}

		//Public Sub PopulateObject(obj As Object)
		//	If (Not obj Is Nothing) Then
		//	todo
		//	End If
		//End Sub

		public void ResetCommand(string sprocName)
		{
			this.ResetCommand(sprocName, CommandType.StoredProcedure);
		}

		public void ResetCommand(string commandText, CommandType commandType)
		{
			if (((this._reader != null)))
			{
				this._reader.Close();
			}
			if (((this._dicOutputParams != null)))
			{
				this._dicOutputParams.Clear();
				this._dicOutputParams = null;
			}
			if (!this._useTransaction)
			{
				if (((this._cmd != null)))
				{
					this._cmd.Dispose();
				}
				this._cmd = new SqlCommand(commandText, this._conn);
				this._cmd.CommandType = commandType;
			}
			else
			{
				this._cmd.Parameters.Clear();
				this._cmd.CommandText = commandText;
				this._cmd.CommandType = commandType;
			}
		}

		public void ResetTransaction()
		{
			this._tran = this._conn.BeginTransaction();
			this._cmd = this._conn.CreateCommand();
			this._cmd.Connection = this._conn;
			this._cmd.Transaction = this._tran;
		}

		public void RollbackTransaction()
		{
			if (((this._tran != null)))
			{
				this._tran.Rollback();
			}
		}

		#endregion

		#region IDisposable Support

		// To detect redundant calls
		private bool _disposedValue;

		// IDisposable

		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposedValue)
			{
				if (disposing)
				{
					if (((this._adptr != null)))
					{
						this._adptr.Dispose();
					}
					if (((this._reader != null)))
					{
						this._reader.Close();
						this._reader.Dispose();
					}
					if (((this._tran != null)))
					{
						this._tran.Dispose();
					}
					if (((this._cmd != null)))
					{
						this._cmd.Dispose();
					}
					if (((this._conn != null)))
					{
						this._conn.Close();
						this._conn.Dispose();
					}
					if (((this._dicOutputParams != null)))
					{
						this._dicOutputParams.Clear();
						this._dicOutputParams = null;
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
				// TODO: set large fields to null.
			}

			this._disposedValue = true;
		}

		// TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
		//Protected Overrides Sub Finalize()
		//    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
		//    Dispose(False)
		//    MyBase.Finalize()
		//End Sub

		// This code added by Visual Basic to correctly implement the disposable pattern.

		public void Dispose()
		{
			// Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
			Dispose(true);
			GC.SuppressFinalize(this);

		}

		#endregion

	}
}
