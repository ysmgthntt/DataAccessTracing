using System.Data;
using System.Data.Common;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

namespace DataAccessTracing.Tests
{
    public class TracingDataAdapterTest
    {
        const int DelayLowMargin = 10;
        const int DelayHighMargin = 50;

        [Fact]
        public void FillAndFillScheme()
        {
            int delay = 200;
            int executingCount = 0, executedCount = 0, unusedCount = 0;
            Guid executionId = default, connectionId = default;
            string? executionMethodName = null;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    connectionId = info.ConnectionId;
                },
                ConnectionClosed = (_) => { },
                BeginTransaction = (_) => { },
                CommandExecuting = (info) =>
                {
                    Assert.Equal(connectionId, info.ConnectionInfo?.ConnectionId);
                    Assert.Equal(nameof(TracingDataAdapterTest), info.CallerMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(FillAndFillScheme), info.CallerMethod?.Name);
                    Assert.Equal(executionMethodName, info.ExecutionMethodName);
                    executionId = info.ExecutionId;
                    executingCount++;
                },
                CommandExecuted = (info) =>
                {
                    Assert.Equal(executionId, info.ExecutionId);
                    Assert.Equal(connectionId, info.ConnectionInfo?.ConnectionId);
                    Assert.Equal(executionMethodName, info.ExecutionMethodName);

                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    Thread.Sleep(DelayLowMargin + DelayHighMargin);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    Assert.Equal(0, info.RecordsAffected);
                    Assert.Null(info.Exception);

                    executedCount++;
                },
                DataReaderClosed = (_) => { },
                UnusedObjectDetected = (info) =>
                {
                    unusedCount++;
                },
            };
            for (int i = 0; i < 2; i++)
            {
                using (var c = new TestConnection())
                using (var tracingConnection = new TracingConnection(c, traceHandler, null))
                using (var cmd = new TestCommand())
                using (var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false))
                using (var da = new TestDataAdapter())
                using (var tracingDataAdapter = new TracingDataAdapter(da, traceHandler))
                {
                    tracingConnection.ConnectionString = "test";
                    tracingConnection.Open();
                    using var trans = tracingConnection.BeginTransaction();
                    tracingCommand.Connection = tracingConnection;
                    tracingCommand.Transaction = trans;

                    var dtTest = new DataTable();
                    {
                        dtTest.Columns.Add("colTest");
                        var newrow = dtTest.NewRow();
                        newrow["colTest"] = "TestValue";
                        dtTest.Rows.Add(newrow);
                    }
                    cmd.ExecutionDelay = delay;
                    cmd.ExecuteReaderDataTable = dtTest;
                    tracingDataAdapter.SelectCommand = tracingCommand;

                    using var ds = new DataSet();
                    Assert.Equal(i, executingCount);
                    Assert.Equal(i, executedCount);
                    if (i == 0)
                    {
                        executionMethodName = "Fill";
                        tracingDataAdapter.Fill(ds);
                    }
                    else
                    {
                        executionMethodName = "FillSchema";
                        tracingDataAdapter.FillSchema(ds, SchemaType.Source);
                    }
                    Assert.Equal(i + 1, executingCount);
                    Assert.Equal(i + 1, executedCount);

#pragma warning disable xUnit2033 // Use the assertion return value instead of re-deriving it
                    Assert.Single(ds.Tables);
                    var dt = ds.Tables[0];
                    if (i == 0)
                    {
                        // Fill
                        Assert.Single(dt.Rows);
                        Assert.Equal("TestValue", dt.Rows[0]["colTest"]);
                    }
                    else
                    {
                        // FillSchema
                        Assert.Empty(dt.Rows);
                        Assert.Single(dt.Columns);
                        Assert.Equal("colTest", dt.Columns[0].ColumnName);
                    }
#pragma warning restore xUnit2033 // Use the assertion return value instead of re-deriving it
                }
            }
            Assert.Equal(0, unusedCount);
        }

        [Fact]
        public void FillAndFillSchemeUsingInnerCommand()
        {
            int delay = 300;
            int executingCount = 0, executedCount = 0, unusedCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (_) => { },
                CommandExecuting = (info) =>
                {
                    executingCount++;
                },
                CommandExecuted = (info) =>
                {
                    executedCount++;
                },
                DataReaderClosed = (_) => { },
                UnusedObjectDetected = (info) =>
                {
                    unusedCount++;
                },
            };
            for (int i = 0; i < 2; i++)
            {
                using (var c = new TestConnection())
                using (var tracingConnection = new TracingConnection(c, traceHandler, null))
                using (var cmd = new TestCommand())
                using (var da = new TestDataAdapter())
                using (var tracingDataAdapter = new TracingDataAdapter(da, traceHandler))
                {
                    tracingConnection.ConnectionString = "test";
                    tracingConnection.Open();
                    using var trans = tracingConnection.BeginTransaction();
                    cmd.Connection = tracingConnection.InnerConnection;
                    cmd.Transaction = ((TracingTransaction)trans).InnerTransaction;

                    var dtTest = new DataTable();
                    {
                        dtTest.Columns.Add("colTest");
                        var newrow = dtTest.NewRow();
                        newrow["colTest"] = "TestValue";
                        dtTest.Rows.Add(newrow);
                    }
                    cmd.ExecutionDelay = delay;
                    cmd.ExecuteReaderDataTable = dtTest;
                    tracingDataAdapter.SelectCommand = cmd;

                    using var ds = new DataSet();
                    Assert.Equal(0, executingCount);
                    Assert.Equal(0, executedCount);
                    if (i == 0)
                        tracingDataAdapter.Fill(ds);
                    else
                        tracingDataAdapter.FillSchema(ds, SchemaType.Source);
                    Assert.Equal(0, executingCount);
                    Assert.Equal(0, executedCount);

#pragma warning disable xUnit2033 // Use the assertion return value instead of re-deriving it
                    Assert.Single(ds.Tables);
                    var dt = ds.Tables[0];
                    if (i == 0)
                    {
                        // Fill
                        Assert.Single(dt.Rows);
                        Assert.Equal("TestValue", dt.Rows[0]["colTest"]);
                    }
                    else
                    {
                        // FillSchema
                        Assert.Empty(dt.Rows);
                        Assert.Single(dt.Columns);
                        Assert.Equal("colTest", dt.Columns[0].ColumnName);
                    }
#pragma warning restore xUnit2033 // Use the assertion return value instead of re-deriving it
                }
                // TracingConnection, TracingTransaction
                Assert.Equal((i + 1) * 2, unusedCount);
            }
        }

        [Fact]
        public void FillAndFillSchemeException()
        {
            int delay = 400;
            int executingCount = 0, executedCount = 0, exceptionCount = 0, unusedCount = 0;
            Guid executionId = default, connectionId = default;
            Exception? exception = null;
            string? executionMethodName = null;
            CommandExecutionInfo? executionInfo = null;
            void CheckCommandExecutionInfoIsDisposed()
            {
                Assert.Null(executionInfo!.Exception);
                var parameterCollection = executionInfo.GetPrivateFieldValue<object>("_parameterCollection");
                Assert.Null(parameterCollection);
            }
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    connectionId = info.ConnectionId;
                },
                ConnectionClosed = (_) => { },
                BeginTransaction = (_) => { },
                CommandExecuting = (info) =>
                {
                    executionId = info.ExecutionId;
                    executingCount++;
                },
                CommandExecuted = (info) =>
                {
                    executedCount++;
                },
                CommandException = (info) =>
                {
                    Assert.Equal(executionId, info.ExecutionId);
                    Assert.Equal(connectionId, info.ConnectionInfo?.ConnectionId);
                    Assert.Equal(executionMethodName, info.ExecutionMethodName);

                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    Thread.Sleep(DelayLowMargin + DelayHighMargin);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    Assert.Equal(0, info.RecordsAffected);

                    Assert.Equal(exception, info.Exception);
                    executionInfo = info;

                    exceptionCount++;
                },
                DataReaderClosed = (_) => { },
                UnusedObjectDetected = (info) =>
                {
                    unusedCount++;
                },
            };
            for (int i = 0; i < 2; i++)
            {
                using (var c = new TestConnection())
                using (var tracingConnection = new TracingConnection(c, traceHandler, null))
                using (var cmd = new TestCommand())
                using (var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false))
                using (var da = new TestDataAdapter())
                using (var tracingDataAdapter = new TracingDataAdapter(da, traceHandler))
                {
                    tracingConnection.ConnectionString = "test";
                    tracingConnection.Open();
                    using var trans = tracingConnection.BeginTransaction();
                    tracingCommand.Connection = tracingConnection;
                    tracingCommand.Transaction = trans;

                    cmd.ExecutionDelay = delay;
                    cmd.ThrowException = exception = new Exception();
                    tracingDataAdapter.SelectCommand = tracingCommand;

                    using var ds = new DataSet();
                    Assert.Equal(i, executingCount);
                    Assert.Equal(0, executedCount);
                    Assert.Equal(i, exceptionCount);
                    if (i == 0)
                    {
                        executionMethodName = "Fill";
                        Assert.Throws<Exception>(() => tracingDataAdapter.Fill(ds));
                    }
                    else
                    {
                        executionMethodName = "FillSchema";
                        Assert.Throws<Exception>(() => tracingDataAdapter.FillSchema(ds, SchemaType.Source));
                    }
                    Assert.Equal(i + 1, executingCount);
                    Assert.Equal(0, executedCount);
                    Assert.Equal(i + 1, exceptionCount);
                    CheckCommandExecutionInfoIsDisposed();

                    Assert.Empty(ds.Tables);
                }
            }
            Assert.Equal(0, unusedCount);
        }

        [Fact]
        public void FillAndFillSchemeCopyTableMappings()
        {
            int unusedCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                DataReaderClosed = (_) => { },
                UnusedObjectDetected = (info) =>
                {
                    unusedCount++;
                },
            };
            for (int i = 0; i < 2; i++)
            {
                using (var c = new TestConnection())
                using (var tracingConnection = new TracingConnection(c, traceHandler, null))
                using (var cmd = new TestCommand())
                using (var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false))
                using (var da = new TestDataAdapter())
                using (var tracingDataAdapter = new TracingDataAdapter(da, traceHandler))
                {
                    tracingConnection.ConnectionString = "test";
                    tracingConnection.Open();
                    using var trans = tracingConnection.BeginTransaction();
                    tracingCommand.Connection = tracingConnection;
                    tracingCommand.Transaction = trans;

                    var dtTest = new DataTable();
                    {
                        dtTest.Columns.Add("colTest");
                        var newrow = dtTest.NewRow();
                        newrow["colTest"] = "TestValue";
                        dtTest.Rows.Add(newrow);
                    }
                    cmd.ExecuteReaderDataTable = dtTest;
                    tracingDataAdapter.SelectCommand = tracingCommand;
                    tracingDataAdapter.TableMappings.Add("Table", "ResultTable");

                    using var ds = new DataSet();
                    Assert.Empty(da.TableMappings);
                    if (i == 0)
                        tracingDataAdapter.Fill(ds);
                    else
                        tracingDataAdapter.FillSchema(ds, SchemaType.Mapped);
                    Assert.Single(da.TableMappings);

#pragma warning disable xUnit2033 // Use the assertion return value instead of re-deriving it
                    Assert.Single(ds.Tables);
                    var dt = ds.Tables[0];
                    if (i == 0)
                    {
                        // Fill
                        Assert.Single(dt.Rows);
                        Assert.Equal("TestValue", dt.Rows[0]["colTest"]);
                    }
                    else
                    {
                        // FillSchema
                        Assert.Empty(dt.Rows);
                        Assert.Single(dt.Columns);
                        Assert.Equal("colTest", dt.Columns[0].ColumnName);
                    }
#pragma warning restore xUnit2033 // Use the assertion return value instead of re-deriving it
                    Assert.Equal("ResultTable", dt.TableName);
                }
            }
            Assert.Equal(0, unusedCount);
        }

        [Fact]
        public void FillSchemeByBase()
        {
            int unusedCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                DataReaderClosed = (_) => { },
                UnusedObjectDetected = (info) =>
                {
                    unusedCount++;
                },
            };
            for (int i = 0; i < 2; i++)
            {
                using (var c = new TestConnection())
                using (var tracingConnection = new TracingConnection(c, traceHandler, null))
                using (var cmd = new TestCommand())
                using (var tracingCommand = new TracingCommand(cmd, traceHandler, tracingConnection, false))
                using (var da = new TestDataAdapter())
                using (var tracingDataAdapter = new TracingDataAdapter(da, traceHandler))
                {
                    tracingConnection.ConnectionString = "test";
                    tracingConnection.Open();
                    using var trans = tracingConnection.BeginTransaction();
                    tracingCommand.Connection = tracingConnection;
                    tracingCommand.Transaction = trans;

                    var dtTest = new DataTable();
                    {
                        dtTest.Columns.Add("colTest");
                        var newrow = dtTest.NewRow();
                        newrow["colTest"] = "TestValue";
                        dtTest.Rows.Add(newrow);
                    }
                    cmd.ExecuteReaderDataTable = dtTest;
                    tracingDataAdapter.SelectCommand = tracingCommand;

                    using var ds = new DataSet();
                    if (i == 0)
                    {
                        var dtResult = new DataTable();
                        ds.Tables.Add(dtResult);
                        tracingDataAdapter.FillSchema(dtResult, SchemaType.Source);
                    }
                    else
                    {
                        tracingDataAdapter.FillSchema(ds, SchemaType.Source, "Table");
                    }

#pragma warning disable xUnit2033 // Use the assertion return value instead of re-deriving it
                    Assert.Single(ds.Tables);
                    var dt = ds.Tables[0];
                    Assert.Empty(dt.Rows);
                    Assert.Single(dt.Columns);
                    Assert.Equal("colTest", dt.Columns[0].ColumnName);
#pragma warning restore xUnit2033 // Use the assertion return value instead of re-deriving it
                }
            }
            Assert.Equal(0, unusedCount);
        }

        [Fact]
        public void Update()
        {
            int unusedCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                DataReaderClosed = (_) => { },
                UnusedObjectDetected = (info) =>
                {
                    unusedCount++;
                },
            };
            for (int i = 0; i < 2; i++)
            {
                using (var c = new TestConnection())
                using (var tracingConnection = new TracingConnection(c, traceHandler, null))
                using (var insertCmd = new TestCommand())
                using (var updateCmd = new TestCommand())
                using (var deleteCmd = new TestCommand())
                using (var insertCommand = new TracingCommand(insertCmd, traceHandler, tracingConnection, false))
                using (var updateCommand = new TracingCommand(updateCmd, traceHandler, tracingConnection, false))
                using (var deleteCommand = new TracingCommand(deleteCmd, traceHandler, tracingConnection, false))
                using (var da = new TestDataAdapter())
                using (var tracingDataAdapter = new TracingDataAdapter(da, traceHandler))
                {
                    tracingConnection.ConnectionString = "test";
                    tracingConnection.Open();
                    using var trans = tracingConnection.BeginTransaction();
                    insertCommand.Connection = tracingConnection;
                    updateCommand.Connection = tracingConnection;
                    deleteCommand.Connection = tracingConnection;
                    insertCommand.Transaction = trans;
                    updateCommand.Transaction = trans;
                    deleteCommand.Transaction = trans;
                    tracingDataAdapter.InsertCommand = insertCommand;
                    tracingDataAdapter.UpdateCommand = updateCommand;
                    tracingDataAdapter.DeleteCommand = deleteCommand;
                    tracingDataAdapter.TableMappings.Add("Table", "TestTable");

                    insertCmd.ExecuteNonQueryResult = 123;
                    updateCmd.ExecuteNonQueryResult = 456;
                    deleteCmd.ExecuteNonQueryResult = 789;

                    var dtTest = new DataTable();
                    {
                        dtTest.TableName = "TestTable";
                        dtTest.Columns.Add("colTest");
                        for (int r = 0; r < 6; r++)
                        {
                            var newrow = dtTest.NewRow();
                            newrow["colTest"] = "TestValue";
                            dtTest.Rows.Add(newrow);
                            if (r >= 2)
                                newrow.AcceptChanges();
                        }
                        dtTest.Rows[2]["colTest"] = "modified1";
                        dtTest.Rows[3]["colTest"] = "modified2";
                        dtTest.Rows[4].Delete();
                        dtTest.Rows[5].Delete();
                    }

                    using var ds = new DataSet();
                    ds.Tables.Add(dtTest);

                    Assert.True(ds.HasChanges());
                    Assert.Empty(da.TableMappings);
                    int result;
                    if (i == 0)
                        // Update by InnerDataAdapter
                        result = tracingDataAdapter.Update(ds);
                    else
                        // Update by base
                        result = tracingDataAdapter.Update(dtTest);
                    Assert.Equal(6, result);
                    Assert.False(ds.HasChanges());
                    if (i == 0)
                        Assert.Single(da.TableMappings);
                    else
                        Assert.Empty(da.TableMappings);
                }
            }
            Assert.Equal(0, unusedCount);
        }

        [Fact]
        public void DisposeTest()
        {
            var traceHandler = new TestTraceHandler();
#pragma warning disable CA2000
            var da = new TestDataAdapter();
#pragma warning restore CA2000
            var tracingDataAdapter = new TracingDataAdapter(da, traceHandler);
            Assert.False(da.IsDisposed);
            tracingDataAdapter.Dispose();
            Assert.True(da.IsDisposed);
        }

        [Fact]
        public void DisposeLeak()
        {
            int leakCount = 0;
            void Test()
            {
                Guid id = default;
                var traceHandler = new TestTraceHandler
                {
                    DisposeLeakDetected = (info) =>
                    {
                        Assert.Equal(DbObjectKind.DataAdapter, info.Kind);
                        Assert.Equal(id, info.Id);
                        Assert.Equal(nameof(TracingDataAdapterTest), info.CreationMethod?.DeclaringType?.Name);
                        Assert.Equal(nameof(DisposeLeak), info.CreationMethod?.Name);
                        leakCount++;
                    },
                };
#pragma warning disable CA2000
                var da = new TestDataAdapter();
                var tracingDataAdapter = new TracingDataAdapter(da, traceHandler);
#pragma warning restore CA2000
                var info = tracingDataAdapter.GetPrivateFieldValue<DataAdapterInfo>("_info");
                id = info?.Id ?? default;
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
                UnusedObjectDetected = (info) =>
                {
                    Assert.Equal(DbObjectKind.DataAdapter, info.Kind);
                    Assert.Equal(id, info.Id);
                    Assert.Equal(nameof(TracingDataAdapterTest), info.CreationMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(Unused), info.CreationMethod?.Name);
                    unusedCount++;
                },
            };
#pragma warning disable CA2000
            TracingDataAdapter CreateTracingDataAdapter() => new(new TestDataAdapter(), traceHandler);
#pragma warning restore CA2000
            using (var tracingDataAdapter = CreateTracingDataAdapter())
            {
                var info = tracingDataAdapter.GetPrivateFieldValue<DataAdapterInfo>("_info");
                id = info?.Id ?? default;
                Assert.Equal(0, unusedCount);
            }
            Assert.Equal(1, unusedCount);
        }

        [Fact]
        public void RowUpdating()
        {
            int rowUpdatingCount = 0;
            var traceHandler = new TestTraceHandler();
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, null, false);
            using var da = new TestDataAdapter();
            using var tracingDataAdapter = new TracingDataAdapter(da, traceHandler);
            using var dt = new DataTable();
            var row = dt.NewRow();
            var tm = new DataTableMapping();
            void TracingDataAdapter_RowUpdating(object? sender, RowUpdatingEventArgs e)
            {
                Assert.Equal(row, e.Row);
                Assert.Equal(cmd, e.Command);
                Assert.Equal(StatementType.Select, e.StatementType);
                Assert.Equal(tm, e.TableMapping);
                rowUpdatingCount++;
            }
            tracingDataAdapter.RowUpdating += TracingDataAdapter_RowUpdating;
            Assert.Equal(0, rowUpdatingCount);
            da.RaiseRowUpdatingEvent(row, cmd, StatementType.Select, tm);
            Assert.Equal(1, rowUpdatingCount);
            da.RaiseRowUpdatingEvent(row, cmd, StatementType.Select, tm);
            Assert.Equal(2, rowUpdatingCount);
            tracingDataAdapter.RowUpdating -= TracingDataAdapter_RowUpdating;
            da.RaiseRowUpdatingEvent(row, cmd, StatementType.Select, tm);
            Assert.Equal(2, rowUpdatingCount);
        }

        [Fact]
        public void RowUpdated()
        {
            int rowUpdatedCount = 0;
            var traceHandler = new TestTraceHandler();
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, null, false);
            using var da = new TestDataAdapter();
            using var tracingDataAdapter = new TracingDataAdapter(da, traceHandler);
            using var dt = new DataTable();
            var row = dt.NewRow();
            var tm = new DataTableMapping();
            void TracingDataAdapter_RowUpdated(object? sender, RowUpdatedEventArgs e)
            {
                Assert.Equal(row, e.Row);
                Assert.Equal(cmd, e.Command);
                Assert.Equal(StatementType.Update, e.StatementType);
                Assert.Equal(tm, e.TableMapping);
                rowUpdatedCount++;
            }
            tracingDataAdapter.RowUpdated += TracingDataAdapter_RowUpdated;
            Assert.Equal(0, rowUpdatedCount);
            da.RaiseRowUpdatedEvent(row, cmd, StatementType.Update, tm);
            Assert.Equal(1, rowUpdatedCount);
            da.RaiseRowUpdatedEvent(row, cmd, StatementType.Update, tm);
            Assert.Equal(2, rowUpdatedCount);
            tracingDataAdapter.RowUpdated -= TracingDataAdapter_RowUpdated;
            da.RaiseRowUpdatedEvent(row, cmd, StatementType.Update, tm);
            Assert.Equal(2, rowUpdatedCount);
        }
    }
}
