﻿(*** hide ***)
#I @"../../files/sqlite"
(*** hide ***)
#I "../../../bin/net451"
(*** hide ***)
#r "../../../packages/scripts/MySqlConnector/lib/net45/MySqlConnector.dll"
(*** hide ***)
#r "FSharp.Data.SqlProvider.dll"
open FSharp.Data.Sql

(*** hide ***)
[<Literal>]
let connectionString = "Data Source=" + __SOURCE_DIRECTORY__ + @"/../../../tests/SqlProvider.Tests/scripts/northwindEF.db;Version=3"

(*** hide ***)
[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__ + @"/../../../tests/SqlProvider.Tests/libs"

(*** hide ***)
type sqlType =
    SqlDataProvider<
        Common.DatabaseProviderTypes.SQLITE,
        connectionString,
        ResolutionPath = resolutionPath
    >

(**


# SQL Provider Static Parameters

## Global parameters

These are the "common" parameters used by all SqlProviders.

All static parameters must be known at compile time, for strings this can be 
achieved by adding the `[<Literal>]` attribute if you are not passing it inline.

### ConnectionString

This is the connection string commonly used to connect to a database server 
instance.  See the documentation on your desired database type to find out 
more. 
*)

[<Literal>]
let sqliteConnectionString =
    "Data Source=" + __SOURCE_DIRECTORY__ + @"\northwindEF.db;Version=3"

(**
### ConnectionStringName

Instead of storing the connection string in the source code / `fsx` script, you
can store values in the `App.config` file:

```xml
<connectionStrings>  
  <add name="MyConnectionString"   
   providerName="System.Data.ProviderName"   
   connectionString="Valid Connection String;" />  
</connectionStrings>
```

Another, usually easier option is to give a runtime connection string as parameter for `.GetDataContext(...)` method.

In your source file:
*)

let connexStringName = "MyConnectionString"

(**
### DatabaseVendor

Select enumeration from `Common.DatabaseProviderTypes` to specify which database
type the provider will be connecting to.
*)

[<Literal>]
let dbVendor = Common.DatabaseProviderTypes.SQLITE

(**
### ResolutionPath

When using database vendors other than SQL Server, Access and ODBC, a third party driver
is required. This parameter should point to an absolute or relative directory where the
relevant assemblies are located. See the database vendor specific page for more details.
*)

[<Literal>]
let resolutionPath =
    __SOURCE_DIRECTORY__ + @"..\..\..\files\sqlite"

(**
#### Note on .NET 5 PublishSingleFile and ResolutionPath

If you are publishing your app using .NET 5's PublishSingleFile mode, the driver will
be loaded from the bundle itself rather than from a separate file on the drive. As such,
the ResolutionPath parameter will not work for the published app, nor will the automatic
assembly resolution implemented within SQLProvider.

SQLProvider attempts to load the assembly from the AppDomain in such case. This means
that your driver's assembly must be loaded by your application for SQLProvider to find
it. To do so, simply use the types of your driver before calling the `.GetDataContext(...)`
method, such as in this example, using MySqlConnector. The specific type you refer
to does not matter.
*)

typeof<MySqlConnector.Logging.MySqlConnectorLogLevel>.Assembly |> ignore
let ctx = sqlType.GetDataContext()

(**
### IndividualsAmount

Number of instances to retrieve when using the [individuals](individuals.html) feature.
Default is 1000.
*)

let indivAmt = 500


(**
### UseOptionTypes

If set to FSharp.Data.Sql.Common.NullableColumnType.OPTION, all nullable fields will be represented by F# option types.  If NO_OPTION, nullable
fields will be represented by the default value of the column type - this is important because
the provider will return 0 instead of null, which might cause problems in some scenarios.

The third option is VALUE_OPTION where nullable fields are represented by ValueOption struct.

*)
[<Literal>]
let useOptionTypes = FSharp.Data.Sql.Common.NullableColumnType.OPTION

(**
### ContextSchemaPath

Defining `ContextSchemaPath` and placing a file with schema information according to the definition
enables offline mode that can be useful when the database is unavailable or slow to connect or access.
Schema information file can be generated by calling design-time method `SaveContextSchema` under `Design Time Commands`:

```fsharp
ctx.``Design Time Commands``.SaveContextSchema
```

This method doesn't affect runtime execution. Note that since SQLProvider loads schema information lazily,
calling `SaveContextSchema` only saves the portion of the database schema that is sufficient to compile
queries referenced in the scope of the current solution or script. Therefore it is recommended to execute 
it after the successful build of the whole solution. Type the method name with parentheses, if you then 
type a dot (.), you should see a tooltip with information when the schema was last saved. Once the schema 
is saved, the outcome of the method execution is stored in memory, so the file will not be overwritten. 
In case the database schema changes and the schema file must be updated, remove the outdated file, reload
the solution and retype or uncomment a call to `SaveContextSchema` to regenerate the schema file.

There is a tool method FSharp.Data.Sql.Common.OfflineTools.mergeCacheFiles to merge multiple files together.

*)

[<Literal>]
let contextSchemaPath =
    __SOURCE_DIRECTORY__ + @".\sqlite.schema"


(**
## Platform Considerations

### MSSQL

TableNames to filter amount of tables.

### Oracle

TableNames to filter amount of tables, and Owner.

#### Owner (Used by Oracle, MySQL and PostgreSQL)

This has different meanings when running queries against different database vendors

For PostgreSQL, this sets the schema name where the target tables belong to. Can be also a list separated by spaces, newlines, commas or semicolons.

For MySQL, this sets the database name (Or schema name, for MySQL, it's the same thing). Can be also a list separated by spaces, newlines, commas or semicolons.

For Oracle, this sets the owner of the scheme.



### SQLite

The additional [SQLiteLibrary parameter](sqlite.html#SQLiteLibrary) can be used to specify
which SQLite library to load.

### PostgreSQL

No extra parameters.

### MySQL

No extra parameters.

### ODBC

No extra parameters.
*)


(**
###Example
It is recommended to use named static parameters in your type provider definition like so

*)
type sql = SqlDataProvider<
            ConnectionString = sqliteConnectionString,
            DatabaseVendor = dbVendor,
            ResolutionPath = resolutionPath,
            UseOptionTypes = useOptionTypes
          >

(**

# SQL Provider Data Context Parameters

Besides the static parameters the `.GetDataContext(...)` method has optional parameters:

* connectionString - The database connection string on runtime.
* resolutionPath - The location to look for dynamically loaded assemblies containing database vendor specific connections and custom types
* transactionOptions - TransactionOptions for the transaction created on SubmitChanges.
* commandTimeout - SQL command timeout. Maximum time for single SQL-command in seconds.
* selectOperations - Execute select-clause operations in SQL database rahter than .NET-side.
			  
*)
