using Netricity.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Netricity.DataUtility.Core
{
	public abstract class BaseDataUtility : IDataUtility
	{
		#region Private Fields

		protected Dictionary<string, DbParameter> _dicOutputParams;

		public abstract DbCommand Command { get; }

		public abstract DbDataAdapter DataAdapter { get; }

      public abstract DbConnection Connection { get; }

      private DbTransaction _tran;

		//private DbDataAdapter _adptr;

		private IDataReader _reader;

		// To detect redundant calls
		private bool _disposedHasBeenCalled;

		private bool _closeConnectionAfterFirstCommand;

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

		#endregion

		#region Constructors

		//public BaseDataUtility()
		//{
		//	this.Command = DbCommand. 
		//}

		#endregion

		#region Methods

		public abstract void NewUpCommand(string commandText);

		public abstract void SetParamSqlDbType(string name, SqlDbType sqlDbType);

		public virtual void AddOutputParam(string name, object value)
		{
			if ((this._dicOutputParams == null))
			{
				this._dicOutputParams = new Dictionary<string, DbParameter>();
			}

			if (!this._dicOutputParams.ContainsKey(name))
			{
				var parameter = this.AddParamWithValue(name, value);
				parameter.Direction = ParameterDirection.InputOutput;

				this._dicOutputParams.Add(name, parameter);
			}
		}

		public abstract DbParameter AddParamWithValue(string name, object value);

		public virtual void AddParam(string name, IEnumerable value)
		{
			if ((value == null) || (value is string))
			{
				this.AddParamWithValue(name, value);
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
					
					this.AddParamWithValue(name, xml.Value);

					writer2 = null;
					serializer = null;
				}
			}
		}

		public virtual void AddParam(string name, object value)
		{
			this.AddParamWithValue(name, value);
		}

		public virtual void AddParam(string name, string value, int maxLen)
		{
			if (value != null && value.Length > maxLen)
			{
				value = value.Substring(0, maxLen);
			}

			this.AddParamWithValue(name, value);
		}

		///// <summary>
		///// Adds a table value parameter to the current command.
		///// </summary>
		///// <param name="name">The name.</param>
		///// <param name="value">The value.</param>
		public virtual void AddTableValueParam(string name, IEnumerable<IDataRecord> value)
		{
			var param = AddParamWithValue(name, value);

			SetParamSqlDbType(name, SqlDbType.Structured);
		}

		private void EnsureFieldExists(string fieldName)
		{
			if (((this._reader[fieldName] == null) || this._reader.IsDBNull(this._reader.GetOrdinal(fieldName))))
			{
				throw new CustomException("{0} cannot find a field called '{1}' containing non-null data from '{2}'", new object[] {
					base.GetType(),
					fieldName,
					this.Command.CommandText
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
					this.Command.CommandText
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
				if (this._tran != null)
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

		public virtual DataSet ExecuteDataSet()
		{
         this.DataAdapter.SelectCommand = this.Command;

			var dataSet = new DataSet();
			this.DataAdapter.Fill(dataSet);

			return dataSet;
		}

		public virtual DataTable ExecuteDataTable()
		{
			DataSet dataSet = this.ExecuteDataSet();

			if (dataSet.Tables.Count > 0)
			{
				return dataSet.Tables[0];
			}

			return null;
		}

		public virtual int ExecuteNonQuery()
		{
			return this.Command.ExecuteNonQuery();
		}

		public virtual int ExecuteNonQuery(int expectedRowsAffected)
		{
			int num = this.ExecuteNonQuery();
			if ((num != expectedRowsAffected))
			{
				throw new CustomException("{0} affected {1} rows; {2} expected.", this.Command.CommandText, num, expectedRowsAffected);
			}
			return num;
		}

		public virtual void ExecuteReader()
		{
			if (this._closeConnectionAfterFirstCommand)
			{
				this._reader = this.Command.ExecuteReader(CommandBehavior.CloseConnection);
			}
			else
			{
				this._reader = this.Command.ExecuteReader();
			}
		}

		/// <summary>
		/// Executes the command, and returns the value of the first column of the first row in the result set, cast to the given type.
		/// Additional columns or rows are ignored.
		/// </summary>
		/// <typeparam name="T">The type of the data in the first column of the first row of the result set.</typeparam>
		/// <returns></returns>
		public virtual T ExecuteScalar<T>()
		{
			object obj2 = this.Command.ExecuteScalar();

         if (obj2 is T)
			{
				return (T)obj2;
			}
			
			return default(T);
		}

		public virtual XmlReader ExecuteXmlReader()
		{
			//return this._cmd.ExecuteXmlReader();
			throw new NotImplementedException();
		}

		public virtual bool GetBool(string fieldName)
		{
			this.EnsureFieldExists(fieldName);
			return ((this._reader[fieldName].ToString() == "1") || (this._reader[fieldName].ToString().ToLower() == "true"));
		}

		public virtual bool GetBool(string fieldName, string trueString)
		{
			this.EnsureFieldExists(fieldName);
			return this.GetBool(fieldName, trueString, true);
		}

		public virtual bool GetBool(string fieldName, string trueString, bool ignoreCase)
		{
			this.EnsureFieldExists(fieldName);
			return (string.Compare(this._reader[fieldName].ToString(), trueString, ignoreCase) == 0);
		}

		public virtual byte[] GetByteArray(string fieldName)
		{
			this.EnsureFieldExistsOrIsNull(fieldName);
			return (byte[])this._reader[fieldName];
		}

		public virtual DateTime GetDateTime(string fieldName)
		{
			this.EnsureFieldExists(fieldName);
			return Convert.ToDateTime(this._reader[fieldName]);
		}

		public virtual double GetDouble(string fieldName)
		{
			this.EnsureFieldExists(fieldName);
			return double.Parse(this._reader[fieldName].ToString());
		}

		public virtual T GetEnum<T>(string fieldName)
		{
			dynamic requestedType = typeof(T);
			if (!requestedType.IsEnum)
			{
				throw new CustomException("T must be an Enum type");
			}
			this.EnsureFieldExistsOrIsNull(fieldName);
			return (T)Enum.Parse(requestedType, this._reader[fieldName].ToString(), true);
		}

		public virtual T GetFromXml<T>(string fieldName)
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

		public virtual int GetInt(string fieldName)
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

		public virtual Nullable<DateTime> GetNullableDateTime(string fieldName)
		{
			int ordinal = this.EnsureFieldExistsOrIsNull(fieldName);
			if (this._reader.IsDBNull(ordinal))
			{
				return null;
			}
			return new Nullable<DateTime>(Convert.ToDateTime(this._reader[fieldName]));
		}

		public virtual Nullable<int> GetNullableInt(string fieldName)
		{
			int ordinal = this.EnsureFieldExistsOrIsNull(fieldName);
			if (this._reader.IsDBNull(ordinal))
			{
				return null;
			}
			return new Nullable<int>(int.Parse(this._reader[fieldName].ToString()));
		}

		public virtual string GetNullableString(string fieldName)
		{
			int ordinal = this.EnsureFieldExistsOrIsNull(fieldName);
			if (this._reader.IsDBNull(ordinal))
			{
				return null;
			}
			return this._reader[fieldName].ToString();
		}

      public virtual T GetOutputParamValue<T>(string paramName)
		{
			var parameter = this._dicOutputParams[paramName];

			return (T)parameter.Value;
		}

      public virtual string GetString(string fieldName)
		{
			this.EnsureFieldExists(fieldName);
			return this._reader[fieldName].ToString();
		}

      public virtual T GetValue<T>(string fieldName, T defaultValue = default(T))
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
			var objType = typeof(T);

			var isNullable = IsNullableType(objType);

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
			if (this._reader == null)
			{
				return false;
			}

			if (!this.ReaderHasRows())
				return false;

			return this._reader.Read();
		}

		public abstract bool ReaderHasRows();

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
			if (this._reader != null)
			{
				this._reader.Close();
			}

			if (this._dicOutputParams != null)
			{
				this._dicOutputParams.Clear();
				this._dicOutputParams = null;
			}

			if (!this._useTransaction)
			{
				if (this.Command != null)
				{
					this.Command.Dispose();
				}
				
				NewUpCommand(commandText);

				this.Command.CommandType = commandType;
			}
			else
			{
				this.Command.Parameters.Clear();
				this.Command.CommandText = commandText;
				this.Command.CommandType = commandType;
			}
		}

		public void ResetTransaction()
		{
			this._tran = this.Connection.BeginTransaction();
			
			NewUpCommand(null);

			this.Command.Connection = this.Connection;
			this.Command.Transaction = this._tran;
		}

      //public abstract void InitDbConnection();

      //public abstract void InitDbAdapter();

      public void RollbackTransaction()
		{
			if (this._tran != null)
			{
				this._tran.Rollback();
			}
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

      public void Dispose()
		{
			// Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
			Dispose(true);
			GC.SuppressFinalize(this);

		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposedHasBeenCalled)
			{
				if (disposing)
				{
					if (this.DataAdapter != null)
					{
						this.DataAdapter.Dispose();
					}

					if (this._reader != null)
					{
						this._reader.Close();
						this._reader.Dispose();
					}

					if (this._tran != null)
					{
						this._tran.Dispose();
					}

					if (this.Command != null)
					{
						this.Command.Dispose();
					}

					if (this._dicOutputParams != null)
					{
						this._dicOutputParams.Clear();
						this._dicOutputParams = null;
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
				// TODO: set large fields to null.
			}

			this._disposedHasBeenCalled = true;
		}

		#endregion
	}
}
