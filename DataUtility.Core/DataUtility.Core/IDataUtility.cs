using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Xml;

namespace Netricity.DataUtility.Core
{
	public interface IDataUtility : IDisposable
	{
		DbCommand Command { get; /*set;*/ }
		void NewUpCommand(string commandText);

		DbParameter AddParamWithValue(string name, object value);
		void AddOutputParam(string name, object value);
		void AddParam(string name, IEnumerable value);
		void AddParam(string name, object value);
		void AddParam(string name, string value, int maxLen);
		void AddTableValueParam(string name, IEnumerable<IDataRecord> value);
		void SetParamSqlDbType(string name, SqlDbType sqlDbType);

		DataSet ExecuteDataSet();
		DataTable ExecuteDataTable();
		int ExecuteNonQuery();
		int ExecuteNonQuery(int expectedRowsAffected);
		void ExecuteReader();
		T ExecuteScalar<T>();
		XmlReader ExecuteXmlReader();
		bool ReaderHasRows();

		bool CheckFieldExists(string fieldName);
		bool GetBool(string fieldName);
		bool GetBool(string fieldName, string trueString);
		bool GetBool(string fieldName, string trueString, bool ignoreCase);
		byte[] GetByteArray(string fieldName);
		DateTime GetDateTime(string fieldName);
		double GetDouble(string fieldName);
		T GetEnum<T>(string fieldName);
		T GetFromXml<T>(string fieldName);
		int GetInt(string fieldName);
		DateTime? GetNullableDateTime(string fieldName);
		int? GetNullableInt(string fieldName);
		string GetNullableString(string fieldName);
		T GetOutputParamValue<T>(string paramName);
		string GetString(string fieldName);
		T GetValue<T>(string fieldName, T defaultValue = default(T));

		bool NextResult();
		bool NextRow();
		IDataReader Reader { get; }
		void ResetCommand(string commandText, CommandType commandType);
		void ResetCommand(string sprocName);

		bool CommitTransaction();
		void ResetTransaction();
		void RollbackTransaction();

		int Timeout { get; set; }
	}
}
