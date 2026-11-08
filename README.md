# DataAccessTracing

**DataAccessTracing** is a .NET library that provides tracing and diagnostics for ADO.NET operations.

It helps you understand how your application uses database connections and commands by tracing SQL execution, monitoring object lifetimes, and detecting potential programming mistakes.

## Features

- **SQL execution tracing**
  - Trace SQL commands executed through ADO.NET.
  - Retrieve command text with parameter values expanded.
  - Measure command execution time and obtain the number of affected records.
  - Capture exceptions raised during command execution.

- **Connection and transaction tracing**
  - Monitor connection open and close operations.
  - Measure connection lifetime.
  - Trace transaction begin, commit, and rollback operations.

- **Object lifetime diagnostics**
  - Detect objects that are not disposed properly.
  - Detect objects that are created but never used.
  - Supports connections, transactions, commands, data readers, data adapters, and command builders.

- **Parameter validation**
  - Detect parameters that are not referenced in the command text.
  - Detect duplicated parameter names.
  - Receive detailed information about invalid parameters.

- **Customizable trace handling**
  - Implement a custom `DataAccessTraceHandler` to handle tracing events.
  - Use the default handler or provide your own implementation.

## Getting Started

DataAccessTracing works by wrapping an existing `DbProviderFactory` with `TracingProviderFactory`.

The wrapped provider continues to create the underlying ADO.NET objects, while DataAccessTracing adds tracing and diagnostic capabilities.

### .NET

In .NET, you can create a `TracingProviderFactory` directly.

The following example uses `Microsoft.Data.SqlClient`.

```csharp
using System.Data.Common;
using Microsoft.Data.SqlClient;
using DataAccessTracing;

DbProviderFactory factory = new TracingProviderFactory(SqlClientFactory.Instance);

using DbConnection connection = factory.CreateConnection();
connection.ConnectionString = "...";
connection.Open();

// Execute database operations as usual.
```

You can also specify a custom trace handler when creating the factory.

```csharp
DbProviderFactory factory = new TracingProviderFactory(SqlClientFactory.Instance, new MyTraceHandler());
```

### .NET Framework

In .NET Framework, `DbProviderFactories.GetFactory()` can be used to obtain a tracing-enabled provider factory.

First, register the provider in your application's configuration file (`App.config` or `Web.config`) as usual.

Then call `ReplaceDbProviderFactoriesWithTracingProviders()` before obtaining the factory.

```csharp
using System.Data.Common;
using DataAccessTracing;

DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();

DbProviderFactory factory = DbProviderFactories.GetFactory("System.Data.SqlClient");

using DbConnection connection = factory.CreateConnection();
connection.ConnectionString = "...";
connection.Open();

// Execute database operations as usual.
```

`ReplaceDbProviderFactoriesWithTracingProviders()` replaces the registered provider factories with tracing-enabled wrappers.

## Configuration

### Default Trace Handler

You can set the default trace handler for tracing-enabled provider factories.

```csharp
DataAccessTrace.SetDefaultTraceHandler(new MyTraceHandler());
```

For factories created directly with `new TracingProviderFactory(...)`, you can specify a trace handler in the constructor instead.

### Parameter Validation

Parameter validation can be enabled by default for tracing-enabled provider factories.

```csharp
DataAccessTrace.IsParameterValidationEnabledDefault = true;
```

You can also configure parameter validation individually for each `TracingProviderFactory`.

```csharp
var factory = new TracingProviderFactory(SqlClientFactory.Instance)
{
    IsParameterValidationEnabled = true
};
```

Parameter validation detects problems such as:

- Parameters that are not referenced in the command text.
- Multiple parameters with the same name.

When an invalid parameter is detected, the trace handler is notified through `OnInvalidParameterDetected`. The default handler throws an `InvalidParameterException`.

## Custom Trace Handler

To customize how tracing events are handled, derive from `DataAccessTraceHandler` and override the methods you need.

```csharp
using DataAccessTracing.Diagnostics;

public class MyTraceHandler : DataAccessTraceHandler
{
    protected override void OnConnectionOpened(ConnectionInfo info)
    {
        // Called when a connection is opened.
    }

    protected override void OnConnectionClosed(ConnectionInfo info)
    {
        // Called when a connection is closed.
    }

    protected override void OnCommandExecuting(CommandExecutionInfo info)
    {
        // Called before a command is executed.
    }

    protected override void OnCommandExecuted(CommandExecutionInfo info)
    {
        // Called after a command is executed.
        // You can access execution details through info.
    }

    protected override void OnCommandException(CommandExecutionInfo info)
    {
        // Called when command execution fails.
    }
}
```

`DataAccessTraceHandler` provides virtual methods for various tracing and diagnostic events. You only need to override the methods relevant to your application.

Available events include:

| Method | Description |
|---|---|
| `OnConnectionOpened` | A connection is opened. |
| `OnConnectionClosed` | A connection is closed. |
| `OnBeginTransaction` | A transaction begins. |
| `OnCommitTransaction` | A transaction is committed. |
| `OnRollbackTransaction` | A transaction is rolled back. |
| `OnCommandExecuting` | Before a command is executed. |
| `OnCommandExecuted` | After a command is executed. |
| `OnCommandException` | A command execution fails. |
| `OnDataReaderClosed` | A data reader is closed. |
| `OnUnusedObjectDetected` | An object is detected as unused. |
| `OnDisposeLeakDetected` | An object is detected as not properly disposed. |
| `OnInvalidParameterDetected` | Invalid command parameters are detected. |

## Exception Handling

When parameter validation is enabled and the default trace handler is used, an `InvalidParameterException` is thrown if invalid parameters are detected.

The exception provides details about the command execution and the invalid parameters.

```csharp
try
{
    // Execute database operations.
}
catch (InvalidParameterException ex)
{
    var executionInfo = ex.ExecutionInfo;
    var invalidParameters = ex.InvalidParameters;
    var reason = ex.Reason;
}
```

## Supported Frameworks

DataAccessTracing targets the following frameworks:

- .NET 7 or later
- .NET Standard 2.1
- .NET Framework 3.5 or later

Note that .NET Framework does not support .NET Standard 2.1.

## Acknowledgements

The design and implementation of DataAccessTracing were inspired in part by [Glimpse](https://github.com/Glimpse/Glimpse), an open-source diagnostics platform for .NET.

Glimpse was a valuable technical reference for understanding ADO.NET provider wrapping and database connection monitoring techniques.

Glimpse is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).

## Documentation

This documentation was prepared with assistance from AI.

## License

DataAccessTracing is licensed under the [MIT License](LICENSE.txt).
