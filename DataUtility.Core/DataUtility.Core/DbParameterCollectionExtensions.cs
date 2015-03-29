using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Netricity.DataUtility.Core
{
	/// <summary>
	/// Extension methods for DbParameterCollection.
	/// </summary>
	public static class DbParameterCollectionExtensions
	{
		//
		// Summary:
		//     Adds a value to the end of the System.Data.Common.DbParameterCollection.
		//
		// Parameters:
		//   parameterName:
		//     The name of the parameter.
		//
		//   value:
		//     The value to be added. Use System.DBNull.Value instead of null, to indicate
		//     a null value.
		//
		// Returns:
		//     A System.Data.Common.DbParameter object.
		public static DbParameter AddWithValue(this DbParameterCollection collection, string name, object value)
		{
			var idx = collection.Add(value);
			var param = collection[idx];
			param.ParameterName = name;
			collection[name].Value = value;
			//param.Direction = ParameterDirection.InputOutput;

			return param;
		}
	}
}
