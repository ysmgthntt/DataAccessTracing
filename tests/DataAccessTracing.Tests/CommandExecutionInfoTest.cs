using System.Data;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

namespace DataAccessTracing.Tests
{
    public class CommandExecutionInfoTest
    {
        private static readonly DataAccessTraceHandler s_traceHandler = new EmptyTraceHandler();
        private sealed class EmptyTraceHandler : DataAccessTraceHandler { }

        [Fact]
        public void GetParameterExpandedCommandText()
        {
            const string sql = "SELECT @P, @PPP, @PP, @日本, @日本語, @日本語コード, @C1, @C1, @C1000, @C10, @C100";
            TestParameterCollection parameters =
            [
                new TestParameter { ParameterName = "@PP", Value = "VALUE1", DbType = DbType.Int32 },
                new TestParameter { ParameterName = "@P", Value = "VALUE2", DbType = DbType.Int32 },
                new TestParameter { ParameterName = "@PPP", Value = "VALUE3", DbType = DbType.Int32 },
                new TestParameter { ParameterName = "@日本語", Value = "VALUE4", DbType = DbType.Int32 },
                new TestParameter { ParameterName = "@日本語コード", Value = "VALUE5", DbType = DbType.Int32 },
                new TestParameter { ParameterName = "@日本", Value = "VALUE6", DbType = DbType.Int32 },
                new TestParameter { ParameterName = "@C1", Value = "VALUE7", DbType = DbType.Int32 },
                new TestParameter { ParameterName = "@C100", Value = "VALUE8", DbType = DbType.Int32 },
                new TestParameter { ParameterName = "@C10", Value = "VALUE9", DbType = DbType.Int32 },
            ];
            var info = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.Text, parameters, "GetParameterExpandedCommandText", s_traceHandler, false);
            var extendedCommandText = info.GetParameterExpandedCommandText();
            Assert.Equal("SELECT VALUE2, VALUE3, VALUE1, VALUE6, VALUE4, VALUE5, VALUE7, VALUE7, VALUE80, VALUE9, VALUE8", extendedCommandText);
        }

        [Fact]
        public void ParameterValidationMissingInCommandText()
        {
            var defaultTraceHandler = new DefaultDataAccessTraceHandler();
            const string sql = "SELECT @P, @PPP, @P, @PPP";
            TestParameterCollection validParams =
            [
                new TestParameter { ParameterName = "@P", Value = "VALUE1" },
                new TestParameter { ParameterName = "@PPP", Value = "VALUE3" },
            ];
            new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.Text, validParams, "ParameterValidationMissingInCommandText", defaultTraceHandler, true)
                .Begin();

            TestParameterCollection invalidParams =
            [
                new TestParameter { ParameterName = "@P", Value = "VALUE1" },
                new TestParameter { ParameterName = "@PP", Value = "VALUE2" },
                new TestParameter { ParameterName = "@PPP", Value = "VALUE3" },
                new TestParameter { ParameterName = "@PPPP", Value = "VALUE4" },
            ];
            {
                int invalidCount = 0;
                CommandExecutionInfo? invalidInfo = null;
                var traceHandler = new TestTraceHandler
                {
                    InvalidParameterDetected = (info) =>
                    {
                        Assert.Equal(InvalidParameterReason.MissingInCommandText, info.Reason);
                        Assert.Equal(invalidInfo, info.ExecutionInfo);
                        Assert.Equal(sql, info.ExecutionInfo.CommandText);
                        Assert.Equal(2, info.InvalidParameters.Length);
                        Assert.Single(info.InvalidParameters, p => p.ParameterName == "@PP");
                        Assert.Single(info.InvalidParameters, p => p.ParameterName == "@PPPP");
                        invalidCount++;
                    },
                };
                invalidInfo = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.Text, invalidParams, "ParameterValidationMissingInCommandText", traceHandler, true);
                Assert.Equal(0, invalidCount);
                invalidInfo.Begin();
                Assert.Equal(1, invalidCount);

                invalidInfo = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.Text, invalidParams, "ParameterValidationMissingInCommandText", defaultTraceHandler, true);
                var ex = Assert.Throws<InvalidParameterException>(invalidInfo.Begin);
                Assert.Equal(InvalidParameterReason.MissingInCommandText, ex.Reason);
                Assert.Equal(invalidInfo, ex.ExecutionInfo);
                Assert.Equal(sql, ex.ExecutionInfo.CommandText);
                Assert.Equal(2, ex.InvalidParameters.Length);
                Assert.Single(ex.InvalidParameters, p => p.ParameterName == "@PP");
                Assert.Single(ex.InvalidParameters, p => p.ParameterName == "@PPPP");
                Assert.Equal(1, invalidCount);
            }

            const string storedProc = "TestProc";
            new CommandExecutionInfo(new CommandInfo(), null, null, storedProc, CommandType.StoredProcedure, invalidParams, "ParameterValidationMissingInCommandText", defaultTraceHandler, true)
                .Begin();
        }

        [Fact]
        public void ParameterValidationDuplicateParameterName()
        {
            var defaultTraceHandler = new DefaultDataAccessTraceHandler();
            string sql = "SELECT @P, @PP, @PPP";
            TestParameterCollection validParams =
            [
                new TestParameter { ParameterName = "@P", Value = "VALUE1" },
                new TestParameter { ParameterName = "@PP", Value = "VALUE2" },
                new TestParameter { ParameterName = "@PPP", Value = "VALUE3" },
            ];
            new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.Text, validParams, "ParameterValidationDuplicateParameterName", defaultTraceHandler, true)
                .Begin();

            TestParameterCollection invalidParams =
            [
                new TestParameter { ParameterName = "@P", Value = "VALUE1" },
                new TestParameter { ParameterName = "@PP", Value = "VALUE2" },
                new TestParameter { ParameterName = "@PPP", Value = "VALUE3" },
                new TestParameter { ParameterName = "@P", Value = "VALUE4" },
                new TestParameter { ParameterName = "@PPP", Value = "VALUE5" },
                new TestParameter { ParameterName = "@P", Value = "VALUE6" },
            ];

            int invalidCount = 0;
            CommandExecutionInfo? invalidInfo = null;
            var traceHandler = new TestTraceHandler
            {
                InvalidParameterDetected = (info) =>
                {
                    Assert.Equal(InvalidParameterReason.DuplicateParameterName, info.Reason);
                    Assert.Equal(invalidInfo, info.ExecutionInfo);
                    Assert.Equal(sql, info.ExecutionInfo.CommandText);
                    Assert.Equal(5, info.InvalidParameters.Length);
                    Assert.Equal(3, info.InvalidParameters.Count(p => p.ParameterName == "@P"));
                    Assert.Equal(2, info.InvalidParameters.Count(p => p.ParameterName == "@PPP"));
                    invalidCount++;
                },
            };
            {
                invalidInfo = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.Text, invalidParams, "ParameterValidationDuplicateParameterName", traceHandler, true);
                Assert.Equal(0, invalidCount);
                invalidInfo.Begin();
                Assert.Equal(1, invalidCount);

                invalidInfo = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.Text, invalidParams, "ParameterValidationDuplicateParameterName", defaultTraceHandler, true);
                var ex = Assert.Throws<InvalidParameterException>(() => invalidInfo.Begin());
                Assert.Equal(InvalidParameterReason.DuplicateParameterName, ex.Reason);
                Assert.Equal(invalidInfo, ex.ExecutionInfo);
                Assert.Equal(sql, ex.ExecutionInfo.CommandText);
                Assert.Equal(5, ex.InvalidParameters.Length);
                Assert.Equal(3, ex.InvalidParameters.Count(p => p.ParameterName == "@P"));
                Assert.Equal(2, ex.InvalidParameters.Count(p => p.ParameterName == "@PPP"));
                Assert.Equal(1, invalidCount);
            }
            {
                sql = "TestProc";
                invalidInfo = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.StoredProcedure, invalidParams, "ParameterValidationDuplicateParameterName", traceHandler, true);
                Assert.Equal(1, invalidCount);
                invalidInfo.Begin();
                Assert.Equal(2, invalidCount);

                invalidInfo = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.StoredProcedure, invalidParams, "ParameterValidationDuplicateParameterName", defaultTraceHandler, true);
                var ex = Assert.Throws<InvalidParameterException>(() => invalidInfo.Begin());
                Assert.Equal(InvalidParameterReason.DuplicateParameterName, ex.Reason);
                Assert.Equal(invalidInfo, ex.ExecutionInfo);
                Assert.Equal(sql, ex.ExecutionInfo.CommandText);
                Assert.Equal(5, ex.InvalidParameters.Length);
                Assert.Equal(3, ex.InvalidParameters.Count(p => p.ParameterName == "@P"));
                Assert.Equal(2, ex.InvalidParameters.Count(p => p.ParameterName == "@PPP"));
                Assert.Equal(2, invalidCount);
            }
        }

        [Theory]
        [InlineData(DbType.AnsiString, true)]
        [InlineData(DbType.Binary, false)]
        [InlineData(DbType.Byte, false)]
        [InlineData(DbType.Boolean, false)]
        [InlineData(DbType.Currency, false)]
        [InlineData(DbType.Date, true)]
        [InlineData(DbType.DateTime, true)]
        [InlineData(DbType.Decimal, false)]
        [InlineData(DbType.Double, false)]
        [InlineData(DbType.Guid, false)]
        [InlineData(DbType.Int16, false)]
        [InlineData(DbType.Int32, false)]
        [InlineData(DbType.Int64, false)]
        [InlineData(DbType.Object, false)]
        [InlineData(DbType.SByte, false)]
        [InlineData(DbType.Single, false)]
        [InlineData(DbType.String, true)]
        [InlineData(DbType.Time, true)]
        [InlineData(DbType.UInt16, false)]
        [InlineData(DbType.UInt32, false)]
        [InlineData(DbType.UInt64, false)]
        [InlineData(DbType.VarNumeric, false)]
        [InlineData(DbType.AnsiStringFixedLength, true)]
        [InlineData(DbType.StringFixedLength, true)]
        [InlineData(DbType.Xml, false)]
        [InlineData(DbType.DateTime2, true)]
        [InlineData(DbType.DateTimeOffset, true)]
        public void DbTypeTest(DbType dbType, bool quote)
        {
            const string sql = "SELECT @P ";
            TestParameterCollection parameters = [new TestParameter { ParameterName = "@P", Value = "value", DbType = dbType }];
            var info = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.Text, parameters, "DbTypeTest", s_traceHandler, false);
            string expected = $"SELECT {(quote ? "'value'" : "value")} ";
            Assert.Equal(expected, info.GetParameterExpandedCommandText());
        }

        [Theory]
        [InlineData(ParameterDirection.Input, true)]
        [InlineData(ParameterDirection.InputOutput, true)]
        [InlineData(ParameterDirection.Output, false)]
        [InlineData(ParameterDirection.ReturnValue, false)]
        public void ParameterDirectionTest(ParameterDirection direction, bool expand)
        {
            const string sql = "SELECT @P ";
            TestParameterCollection parameters = [new TestParameter { ParameterName = "@P", Value = "value", Direction = direction }];
            var info = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.Text, parameters, "ParameterDirectionTest", s_traceHandler, false);
            string expected = expand ? "SELECT 'value' " : sql;
            Assert.Equal(expected, info.GetParameterExpandedCommandText());
        }

        [Fact]
        public void ParameterValueTest()
        {
            static void Test(string expected, object? value, DbType dbType = DbType.AnsiString)
            {
                const string sql = "SELECT @P ";
                TestParameterCollection parameters = [new TestParameter { ParameterName = "@P", Value = value, DbType = dbType }];
                var info = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.Text, parameters, "ParameterValueTest", s_traceHandler, false);
                Assert.Equal(expected, info.GetParameterExpandedCommandText());
            }
            Test("SELECT NULL ", null);
            Test("SELECT NULL ", DBNull.Value);
            Test("SELECT '1234' ", 1234);
            Test("SELECT 1234 ", 1234, DbType.Int32);
            Test("SELECT 'Saturday' ", DayOfWeek.Saturday);
            Test("SELECT 6/*Saturday*/ ", DayOfWeek.Saturday, DbType.Int32);
            Test("SELECT 6/*Saturday*/ ", DayOfWeek.Saturday, DbType.Decimal);
        }

        [Fact]
        public void GetStoredProcCommandTextTest()
        {
            const string sql = "TestProc";
            TestParameterCollection parameters =
            [
                new TestParameter { ParameterName = "@Param1", Value = "VALUE1", DbType = DbType.AnsiString, Direction = ParameterDirection.Input },
                new TestParameter { ParameterName = "@Param2", Value = "VALUE2", DbType = DbType.Decimal, Direction = ParameterDirection.Output },
                new TestParameter { ParameterName = "@Param3", Value = "VALUE3", DbType = DbType.Object, Direction = ParameterDirection.InputOutput },
                new TestParameter { ParameterName = "@Param4", Value = "VALUE4", DbType = DbType.Boolean, Direction = ParameterDirection.ReturnValue },
            ];
            var info = new CommandExecutionInfo(new CommandInfo(), null, null, sql, CommandType.StoredProcedure, parameters, "GetStoredProcCommandTextTest", s_traceHandler, false);
            var extendedCommandText = info.GetParameterExpandedCommandText();
            var nl = Environment.NewLine;
            Assert.Equal("TestProc"
                + nl + "@Param1 (AnsiString): VALUE1"
                + nl + "@Param2 (Decimal Output): VALUE2"
                + nl + "@Param3 (Object InputOutput): VALUE3"
                + nl + "@Param4 (Boolean ReturnValue): VALUE4"
                , extendedCommandText);
        }
    }
}
