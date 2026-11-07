using System.Data;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

#pragma warning disable CA2007

namespace DataAccessTracing.Tests
{
    public class TracingDataReaderTest
    {
        [Fact]
#if NET35
        public void GetTestNet35() => GetTest().GetAwaiter().GetResult();
#endif
        public async Task GetTest()
        {
            using var dt = new DataTable();
            dt.Columns.Add("colObject", typeof(object));
            dt.Columns.Add("colBool", typeof(bool));
            dt.Columns.Add("colByte", typeof(byte));
            dt.Columns.Add("colBytes", typeof(byte[]));
            dt.Columns.Add("colChar", typeof(char));
            dt.Columns.Add("colChars", typeof(char[]));
            dt.Columns.Add("colDateTime", typeof(DateTime));
            dt.Columns.Add("colDecimal", typeof(decimal));
            dt.Columns.Add("colDouble", typeof(double));
            dt.Columns.Add("colFloat", typeof(float));
            dt.Columns.Add("colGuid", typeof(Guid));
            dt.Columns.Add("colInt16", typeof(Int16));
            dt.Columns.Add("colInt32", typeof(Int32));
            dt.Columns.Add("colInt64", typeof(Int64));
            dt.Columns.Add("colString", typeof(string));
            dt.Columns.Add("colDBNull", typeof(string));

            object obj = new object();
            DateTime dtm = DateTime.Now;
            Guid guid = Guid.NewGuid();
            {
                var row = dt.NewRow();
                row["colObject"] = obj;
                row["colBool"] = true;
                row["colByte"] = 123;
                row["colBytes"] = (byte[])[1, 12, 123, 127];
                row["colChar"] = 'X';
                row["colChars"] = (char[])['A', 'あ', 'Z', 'ん'];
                row["colDateTime"] = dtm;
                row["colDecimal"] = 123456789012345m;
                row["colDouble"] = 12345.6789;
                row["colFloat"] = 123.45;
                row["colGuid"] = guid;
                row["colInt16"] = 1616;
                row["colInt32"] = 3232;
                row["colInt64"] = 6464;
                row["colString"] = "TestStringValue";
                dt.Rows.Add(row);
            }
            Guid executionId = default;
            int readerCloseCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (info) =>
                {
                    executionId = info.ExecutionId;
                },
                DataReaderClosed = (info) =>
                {
                    Assert.Equal(DbObjectKind.DataReader, info.Kind);
                    Assert.Equal(executionId, info.ExecutionId);
                    Assert.Equal(1, info.RowCount);

                    var duration = info.Duration.TotalMilliseconds;
                    Assert.True(duration > 400);
                    Thread.Sleep(100);
                    Assert.Equal(duration, info.Duration.TotalMilliseconds);

                    readerCloseCount++;
                }
            };
            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            tracingCommand.Connection = tracingConnection;
            cmd.ExecuteReaderDataTable = dt;
            using (var dr = tracingCommand.ExecuteReader())
            {
                if (!dr.Read())
                    Assert.Fail();

                Assert.Equal('X', dr[4]);
                Assert.Equal("TestStringValue", dr["colString"]);
                Assert.True(dr.GetBoolean(dr.GetOrdinal("colBool")));
                Assert.Equal((byte)123, dr.GetByte(dr.GetOrdinal("colByte")));
                byte[] buffer = new byte[10];
                dr.GetBytes(dr.GetOrdinal("colBytes"), 1, buffer, 0, 2);
                Assert.Equal((byte)12, buffer[0]);
                Assert.Equal((byte)123, buffer[1]);
                Assert.Equal((byte)0, buffer[2]);
                Assert.Equal('X', dr.GetChar(dr.GetOrdinal("colChar")));
                char[] charBuffer = new char[10];
                dr.GetChars(dr.GetOrdinal("colChars"), 1, charBuffer, 0, 2);
                Assert.Equal('あ', charBuffer[0]);
                Assert.Equal('Z', charBuffer[1]);
                Assert.Equal((char)0, charBuffer[2]);
                Assert.Equal(dtm, dr.GetDateTime(dr.GetOrdinal("colDateTime")));
                Assert.Equal(123456789012345m, dr.GetDecimal(dr.GetOrdinal("colDecimal")));
                Assert.Equal((double)12345.6789, dr.GetDouble(dr.GetOrdinal("colDouble")));
                Assert.Equal((float)123.45, dr.GetFloat(dr.GetOrdinal("colFloat")));
                Assert.Equal(guid, dr.GetGuid(dr.GetOrdinal("colGuid")));
                Assert.Equal(1616, dr.GetInt16(dr.GetOrdinal("colInt16")));
                Assert.Equal(3232, dr.GetInt32(dr.GetOrdinal("colInt32")));
                Assert.Equal(6464, dr.GetInt64(dr.GetOrdinal("colInt64")));
                Assert.Equal("TestStringValue", dr.GetString(dr.GetOrdinal("colString")));
                Assert.Equal(obj, dr.GetValue(dr.GetOrdinal("colObject")));
                Assert.True(dr.IsDBNull(dr.GetOrdinal("colDBNull")));

                if (dr.Read())
                    Assert.Fail();

                Assert.Equal(0, readerCloseCount);
                await Task.Delay(500, TestContext.Current.CancellationToken);
            }
            Assert.Equal(1, readerCloseCount);
        }

        [Fact]
        public void RowCount()
        {
            using var dt = new DataTable();
            dt.Columns.Add("colTest");
            for (int i = 0; i < 37; i++)
            {
                var row = dt.NewRow();
                row["colTest"] = "test";
                dt.Rows.Add(row);
            }

            Guid executionId = default;
            int readerCloseCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (info) =>
                {
                    executionId = info.ExecutionId;
                },
                DataReaderClosed = (info) =>
                {
                    Assert.Equal(DbObjectKind.DataReader, info.Kind);
                    Assert.Equal(executionId, info.ExecutionId);
                    Assert.Equal(37, info.RowCount);
                    readerCloseCount++;
                }
            };

            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            tracingCommand.Connection = tracingConnection;
            cmd.ExecuteReaderDataTable = dt;
            using (var dr = tracingCommand.ExecuteReader())
            {
                while (dr.Read())
#if !NET35
                    dr.GetFieldValue<string>(0);
#else
                    dr.GetValue(0);
#endif
                Assert.Equal(0, readerCloseCount);
            }
            Assert.Equal(1, readerCloseCount);
        }

        [Fact]
#if !NET35
        public async Task RowCountAsync()
        {
            using var dt = new DataTable();
            dt.Columns.Add("colTest");
            for (int i = 0; i < 42; i++)
            {
                var row = dt.NewRow();
                row["colTest"] = "test";
                dt.Rows.Add(row);
            }

            Guid executionId = default;
            int readerCloseCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (info) =>
                {
                    executionId = info.ExecutionId;
                },
                DataReaderClosed = (info) =>
                {
                    Assert.Equal(DbObjectKind.DataReader, info.Kind);
                    Assert.Equal(executionId, info.ExecutionId);
                    Assert.Equal(42, info.RowCount);
                    readerCloseCount++;
                }
            };

            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            tracingCommand.Connection = tracingConnection;
            cmd.ExecuteReaderDataTable = dt;
            using (var dr = await tracingCommand.ExecuteReaderAsync(TestContext.Current.CancellationToken))
            {
                while (await dr.ReadAsync(TestContext.Current.CancellationToken))
                    await dr.GetFieldValueAsync<string>(0, TestContext.Current.CancellationToken);
                Assert.Equal(0, readerCloseCount);
            }
            Assert.Equal(1, readerCloseCount);
        }
#else
        public void RowCountAsync() { }
#endif

        [Fact]
        public void Close()
        {
            using var dt = new DataTable();
            dt.Columns.Add("colTest");
            var row = dt.NewRow();
            row["colTest"] = DBNull.Value;
            dt.Rows.Add(row);
            int readerCloseCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                DataReaderClosed = (info) =>
                {
                    readerCloseCount++;
                }
            };

            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            tracingCommand.Connection = tracingConnection;
            cmd.ExecuteReaderDataTable = dt;
            using (var dr = tracingCommand.ExecuteReader())
            {
                while (dr.Read())
                    ;
                Assert.Equal(0, readerCloseCount);
                dr.Close();
                Assert.Equal(1, readerCloseCount);
            }
            Assert.Equal(1, readerCloseCount);
        }

        [Fact]
#if NET
        public async Task CloseAsync()
        {
            using var dt = new DataTable();
            dt.Columns.Add("colTest");
            var row = dt.NewRow();
            row["colTest"] = DBNull.Value;
            dt.Rows.Add(row);
            int readerCloseCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                DataReaderClosed = (info) =>
                {
                    readerCloseCount++;
                }
            };

            await using var c = new TestConnection();
            await using var tracingConnection = new TracingConnection(c, traceHandler, null);
            await using var cmd = new TestCommand();
            await using var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            tracingCommand.Connection = tracingConnection;
            cmd.ExecuteReaderDataTable = dt;
            await using (var dr = await tracingCommand.ExecuteReaderAsync(TestContext.Current.CancellationToken))
            {
                while (await dr.ReadAsync(TestContext.Current.CancellationToken))
                    ;
                Assert.Equal(0, readerCloseCount);
                await dr.CloseAsync();
                Assert.Equal(1, readerCloseCount);
            }
            Assert.Equal(1, readerCloseCount);
        }
#else
        public void CloseAsync() { }
#endif

        [Fact]
        public void DisposeTest()
        {
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                DataReaderClosed = (_) => { },
            };
            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            using var tracingCommand = tracingConnection.CreateCommand();
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            var tracingDataReader = tracingCommand.ExecuteReader();
            var innerDataReader = tracingDataReader.GetPrivateFieldValue<TestDataReader>("_innerReader")!;
            Assert.False(innerDataReader.IsDisposed);
            tracingDataReader.Dispose();
            Assert.True(innerDataReader.IsDisposed);
        }

        [Fact]
#if NET
        public async Task DisposeAsyncTest()
        {
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                DataReaderClosed = (_) => { },
            };
            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            using var tracingCommand = tracingConnection.CreateCommand();
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            var tracingDataReader = tracingCommand.ExecuteReader();
            var innerDataReader = tracingDataReader.GetPrivateFieldValue<TestDataReader>("_innerReader")!;
            Assert.False(innerDataReader.IsDisposed);
            await tracingDataReader.DisposeAsync();
            Assert.True(innerDataReader.IsDisposed);
        }
#else
        public void DisposeAsyncTest() { }
#endif

        [Fact]
        public void DisposeWithCloseConnection()
        {
            int leakCount = 0;
            void Test(CommandBehavior commandBehavior)
            {
                var traceHandler = new TestTraceHandler
                {
                    ConnectionOpened = (_) => { },
                    ConnectionClosed = (_) => { },
                    CommandExecuting = (_) => { },
                    CommandExecuted = (_) => { },
                    DataReaderClosed = (_) => { },
                    DisposeLeakDetected = (info) =>
                    {
                        Assert.Equal(DbObjectKind.Connection, info.Kind);
                        leakCount++;
                    },
                };
#pragma warning disable CA2000
                var c = new TestConnection();
                var tracingConnection = new TracingConnection(c, traceHandler, null);
#pragma warning restore CA2000
                using var tracingCommand = tracingConnection.CreateCommand();
                tracingConnection.ConnectionString = "test";
                tracingConnection.Open();
                using var tracingDataReader = tracingCommand.ExecuteReader(commandBehavior);
                Assert.Equal(0, leakCount);
            }
            Test(CommandBehavior.CloseConnection);
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            Assert.Equal(0, leakCount);

            Test(CommandBehavior.Default);
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            Assert.Equal(1, leakCount);
        }

        [Fact]
#if NET
        public async Task DisposeAsyncWithCloseConnection()
        {
            int leakCount = 0;
            async Task Test(CommandBehavior commandBehavior)
            {
                var traceHandler = new TestTraceHandler
                {
                    ConnectionOpened = (_) => { },
                    ConnectionClosed = (_) => { },
                    CommandExecuting = (_) => { },
                    CommandExecuted = (_) => { },
                    DataReaderClosed = (_) => { },
                    DisposeLeakDetected = (info) =>
                    {
                        Assert.Equal(DbObjectKind.Connection, info.Kind);
                        leakCount++;
                    },
                };
#pragma warning disable CA2000
                var c = new TestConnection();
                var tracingConnection = new TracingConnection(c, traceHandler, null);
#pragma warning restore CA2000
                await using var tracingCommand = tracingConnection.CreateCommand();
                tracingConnection.ConnectionString = "test";
                tracingConnection.Open();
                await using var tracingDataReader = await tracingCommand.ExecuteReaderAsync(commandBehavior);
                Assert.Equal(0, leakCount);
            }
            await Test(CommandBehavior.CloseConnection);
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            Assert.Equal(0, leakCount);

            await Test(CommandBehavior.Default);
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            Assert.Equal(1, leakCount);
        }
#else
        public void DisposeAsyncWithCloseConnection() { }
#endif

        [Fact]
        public void DisposeLeak()
        {
            int leakCount = 0;
            void Test()
            {
                Guid id = default;
                var traceHandler = new TestTraceHandler
                {
                    ConnectionOpened = (_) => { },
                    ConnectionClosed = (_) => { },
                    CommandExecuting = (_) => { },
                    CommandExecuted = (_) => { },
                    DataReaderClosed = (_) => { },
                    DisposeLeakDetected = (info) =>
                    {
                        Assert.Equal(DbObjectKind.DataReader, info.Kind);
                        Assert.Equal(id, info.Id);
                        Assert.Equal(nameof(TracingDataReaderTest), info.CreationMethod?.DeclaringType?.Name);
                        Assert.Equal(nameof(DisposeLeak), info.CreationMethod?.Name);
                        leakCount++;
                    },
                };
                using var innerConnection = new TestConnection();
                using var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
                using var tracingCommand = tracingConnection.CreateCommand();
                tracingConnection.ConnectionString = "test";
                tracingConnection.Open();
                var tracingDataReader = tracingCommand.ExecuteReader();
                var info = tracingDataReader.GetPrivateFieldValue<DataReaderInfo>("_info");
                id = info?.Id ?? default;
                tracingDataReader.Close();
                Assert.Equal(0, leakCount);
            }
            Test();
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            Assert.Equal(1, leakCount);
        }

        [Fact]
        public void Unused()
        {
            int unusedCount = 0;
            Guid id = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                DataReaderClosed = (_) => { },
                UnusedObjectDetected = (info) =>
                {
                    Assert.Equal(DbObjectKind.DataReader, info.Kind);
                    Assert.Equal(id, info.Id);
                    Assert.Equal(nameof(TracingDataReaderTest), info.CreationMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(Unused), info.CreationMethod?.Name);
                    unusedCount++;
                },
            };
            using var innerConnection = new TestConnection();
            using var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            tracingCommand.Connection = tracingConnection;
            cmd.ExecuteReaderDataTable = new DataTable();
            {
                var tracingDataReader = tracingCommand.ExecuteReader();
                var info = tracingDataReader.GetPrivateFieldValue<DataReaderInfo>("_info");
                id = info?.Id ?? default;

                tracingDataReader.Read();
                tracingDataReader.Close();

                Assert.Equal(0, unusedCount);
                tracingDataReader.Dispose();
                Assert.Equal(0, unusedCount);
            }
            {
                var tracingDataReader = tracingCommand.ExecuteReader();
                var info = tracingDataReader.GetPrivateFieldValue<DataReaderInfo>("_info");
                id = info?.Id ?? default;

                tracingDataReader.Close();

                Assert.Equal(0, unusedCount);
                tracingDataReader.Dispose();
                Assert.Equal(1, unusedCount);
            }
        }

        [Fact]
#if NET
        public async Task UnusedAsync()
        {
            int unusedCount = 0;
            Guid id = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                DataReaderClosed = (_) => { },
                UnusedObjectDetected = (info) =>
                {
                    Assert.Equal(DbObjectKind.DataReader, info.Kind);
                    Assert.Equal(id, info.Id);
                    //Assert.Equal(nameof(TracingDataReaderTest), info.CreationMethod?.DeclaringType?.Name);
                    //Assert.Equal(nameof(UnusedAsync), info.CreationMethod?.Name);
                    unusedCount++;
                },
            };
            await using var innerConnection = new TestConnection();
            await using var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
            await using var cmd = new TestCommand();
            await using var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            tracingCommand.Connection = tracingConnection;
            cmd.ExecuteReaderDataTable = new DataTable();
            {
                var tracingDataReader = await tracingCommand.ExecuteReaderAsync(TestContext.Current.CancellationToken);
                var info = tracingDataReader.GetPrivateFieldValue<DataReaderInfo>("_info");
                id = info?.Id ?? default;

                await tracingDataReader.ReadAsync(TestContext.Current.CancellationToken);
                await tracingDataReader.CloseAsync();

                Assert.Equal(0, unusedCount);
                await tracingDataReader.DisposeAsync();
                Assert.Equal(0, unusedCount);
            }
            {
                var tracingDataReader = await tracingCommand.ExecuteReaderAsync(TestContext.Current.CancellationToken);
                var info = tracingDataReader.GetPrivateFieldValue<DataReaderInfo>("_info");
                id = info?.Id ?? default;

                await tracingDataReader.CloseAsync();

                Assert.Equal(0, unusedCount);
                await tracingDataReader.DisposeAsync();
                Assert.Equal(1, unusedCount);
            }
        }
#else
        public void UnusedAsync() { }
#endif
    }
}
