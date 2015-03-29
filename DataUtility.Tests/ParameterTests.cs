using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Netricity.DataUtility.Core;
using NUnit.Framework;

namespace Netricity.DataUtility.Tests
{
	[TestFixture()]
	public class ParameterTests
	{
		[Test()]
		public void AddWithValue_AddsParameter_WithCorrectNameAndDirection()
		{
			IDataUtility util = new SqlDataUtility();
			util.AddParam("Field1", "Foo");

			var param = util.Command.Parameters["Field1"];

			Assert.NotNull(param);
			Assert.AreEqual(ParameterDirection.Input, param.Direction);
		}

		[Test()]
		public void AddParamWithMaxLen_AddsParameter_WithCorrectLength()
		{
			IDataUtility util = new SqlDataUtility();
			util.AddParam("Field1", "Foo", 2);

			var param = util.Command.Parameters["Field1"];

			Assert.NotNull(param);
			Assert.AreEqual(ParameterDirection.Input, param.Direction);
			Assert.AreEqual(2, param.Value.ToString().Length);
		}

		[Test()]
		public void AddOutputParam_AddsParameter_WithCorrectNameAndDirection()
		{
			IDataUtility util = new SqlDataUtility();
			util.AddOutputParam("Field1", "Foo");

			var param = util.Command.Parameters["Field1"];

			Assert.NotNull(param);
			Assert.AreEqual(ParameterDirection.InputOutput, param.Direction);

			var paramValue = param.Value;
			Assert.AreEqual("Foo", paramValue);
		}

		[Test()]
		public void AddTableValueParam_AddsParameter_WithCorrectNameAndSqlDbType()
		{
			IDataUtility util = new SqlDataUtility();

			var dataRecordCollection = new MockDataRecordCollection();

			util.AddTableValueParam("Settings", dataRecordCollection.FetchItems());

			var param = (SqlParameter)util.Command.Parameters["Settings"];

			Assert.NotNull(param);
			Assert.AreEqual(ParameterDirection.Input, param.Direction);
			Assert.AreEqual(SqlDbType.Structured, param.SqlDbType);
		}
	}
}
