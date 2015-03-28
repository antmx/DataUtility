using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Netricity.DataUtility.Core
{
	public abstract class BaseDataUtility : IDataUtility
	{
		#region Private Fields

		protected Dictionary<string, DbParameter> _dicOutputParams;

		protected DbCommand _cmd;

		#endregion

		public abstract void AddOutputParam(string name, object value);

		public abstract void AddParam(string name, IEnumerable value);

		public void AddParam(string name, object value)
		{
			throw new NotImplementedException();
		}

		public void AddParam(string name, string value, int maxLen)
		{
			throw new NotImplementedException();
		}

		public void AddTableValueParam(string name, IEnumerable<IDataRecord> value)
		{
			throw new NotImplementedException();
		}

		public bool CheckFieldExists(string fieldName)
		{
			throw new NotImplementedException();
		}

		public bool CommitTransaction()
		{
			throw new NotImplementedException();
		}

		public DataSet ExecuteDataSet()
		{
			throw new NotImplementedException();
		}

		public DataTable ExecuteDataTable()
		{
			throw new NotImplementedException();
		}

		public int ExecuteNonQuery()
		{
			throw new NotImplementedException();
		}

		public int ExecuteNonQuery(int expectedRowsAffected)
		{
			throw new NotImplementedException();
		}

		public void ExecuteReader()
		{
			throw new NotImplementedException();
		}

		public T ExecuteScalar<T>()
		{
			throw new NotImplementedException();
		}

		public XmlReader ExecuteXmlReader()
		{
			throw new NotImplementedException();
		}

		public bool GetBool(string fieldName)
		{
			throw new NotImplementedException();
		}

		public bool GetBool(string fieldName, string trueString)
		{
			throw new NotImplementedException();
		}

		public bool GetBool(string fieldName, string trueString, bool ignoreCase)
		{
			throw new NotImplementedException();
		}

		public byte[] GetByteArray(string fieldName)
		{
			throw new NotImplementedException();
		}

		public DateTime GetDateTime(string fieldName)
		{
			throw new NotImplementedException();
		}

		public double GetDouble(string fieldName)
		{
			throw new NotImplementedException();
		}

		public T GetEnum<T>(string fieldName)
		{
			throw new NotImplementedException();
		}

		public T GetFromXml<T>(string fieldName)
		{
			throw new NotImplementedException();
		}

		public int GetInt(string fieldName)
		{
			throw new NotImplementedException();
		}

		public DateTime? GetNullableDateTime(string fieldName)
		{
			throw new NotImplementedException();
		}

		public int? GetNullableInt(string fieldName)
		{
			throw new NotImplementedException();
		}

		public string GetNullableString(string fieldName)
		{
			throw new NotImplementedException();
		}

		public virtual T GetOutputParamValue<T>(string paramName)
		{
			var parameter = this._dicOutputParams[paramName];
			return (T)parameter.Value;
		}

		public string GetString(string fieldName)
		{
			throw new NotImplementedException();
		}

		public T GetValue<T>(string fieldName, T defaultValue = default(T))
		{
			throw new NotImplementedException();
		}

		public bool NextResult()
		{
			throw new NotImplementedException();
		}

		public bool NextRow()
		{
			throw new NotImplementedException();
		}

		public IDataReader Reader
		{
			get { throw new NotImplementedException(); }
		}

		public void ResetCommand(string commandText, CommandType commandType)
		{
			throw new NotImplementedException();
		}

		public void ResetCommand(string sprocName)
		{
			throw new NotImplementedException();
		}

		public void ResetTransaction()
		{
			throw new NotImplementedException();
		}

		public void RollbackTransaction()
		{
			throw new NotImplementedException();
		}

		public int Timeout
		{
			get;
			set;
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
