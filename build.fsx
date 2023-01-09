#r "paket: groupref build //"
//#load "docs/CLI.fs"

#r "./packages/build/System.Data.SqlClient/lib/netstandard2.0/System.Data.SqlClient.dll"

#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/fsharp/FAKE/issues/1985
#endif

open Fake
open Fake.SystemHelper
open Fake.Core.TargetOperators
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Api
open Fake.Tools
open Fake.Tools.Git
open System
open System.IO

Target.initEnvironment()
//BuildServer.install [
//    AppVeyor.Installer
//    Travis.Installer
//]

#if MONO
#else
//#load @"packages/Build/SourceLink.Fake/tools/SourceLink.fsx"
#endif

//#r @"packages/tests/Npgsql/lib/net451/Npgsql.dll"
#I @"./packages/build/System.Threading.Tasks.Extensions/lib/netstandard2.0/"
#r @"./packages/tests/Npgsql/lib/netstandard2.0/Npgsql.dll"
let environVarOrDefault varName defaultValue =

    try
        let envvar = (Environment.environVar varName).ToUpper()
        if String.IsNullOrEmpty envvar then defaultValue else envvar
    with
    | _ ->  defaultValue



// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')

let project = "SQLProvider"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Type providers for SQL database access."

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = "Type providers for SQL database access."

// List of author names (for NuGet package)
let authors = [ "Ross McKinlay, Colin Bull, Tuomas Hietanen" ]

// Tags for your project (for NuGet package)
let tags = "F#, fsharp, typeprovider, sql, sqlserver, mysql, sql-server, sqlite, postgresql, oracle, mariadb, firebirdsql, database, dotnet"

// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin/Release/*Tests*.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "fsprojects"
let gitHome = "https://github.com/" + gitOwner

// The name of the project on GitHub
let gitName = "SQLProvider"

// The url for the raw files hosted
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/fsprojects"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------
// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = ReleaseNotes.load "docs/RELEASE_NOTES.md"

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" (fun _ ->
  let fileName = "src/Common/AssemblyInfo.fs"
  Fake.DotNet.AssemblyInfoFile.createFSharp fileName
      [ Fake.DotNet.AssemblyInfo.Title project
        Fake.DotNet.AssemblyInfo.Product project
        Fake.DotNet.AssemblyInfo.Description summary
        Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
        Fake.DotNet.AssemblyInfo.FileVersion release.AssemblyVersion ] 
)

// --------------------------------------------------------------------------------------
// Clean build results

Target.create "Clean" (fun _ ->
    !! "**/**/bin/" |> Shell.cleanDirs
    !! "**/**/temp/" |> Shell.cleanDirs
    !! "**/**/obj/" |> Shell.cleanDirs
    !! "**/**/test*/**/obj/" |> Shell.cleanDirs
    
    Shell.cleanDirs ["bin"; "temp"]
)

Target.create "CleanDocs" (fun _ ->
    Shell.cleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" (fun _ ->
    Fake.DotNet.DotNet.build (fun p -> {p with Configuration = DotNet.BuildConfiguration.Release}) "SQLProvider.sln"
)

Target.create "BuildTests" (fun _ ->
    // Build the dacpac
    let dacpacSln = System.IO.Path.Combine [| "tests"; "SqlProvider.Core.Tests"; "MsSqlSsdt"; "MsSqlSsdtTests.sln" |]
    Fake.DotNet.DotNet.exec id "build" ( dacpacSln + " -c Debug  --no-dependencies") |> ignore

    // Todo: Change when command-line NuGet works (6.4.0 hopefully)
    Fake.DotNet.DotNet.exec id "build" "SQLProvider.Tests.sln -c Debug  --no-dependencies" |> ignore
    //Fake.DotNet.DotNet.build (fun p -> {p with Configuration = DotNet.BuildConfiguration.Debug}) "SQLProvider.Tests.sln"
)

// --------------------------------------------------------------------------------------
// Set up a PostgreSQL database in the CI pipeline to run tests

Target.create "SetupPostgreSQL" (fun _ ->
    (*
      let connBuilder = Npgsql.NpgsqlConnectionStringBuilder()

      connBuilder.Host <- "localhost"
      connBuilder.Port <- 5432
      connBuilder.Database <- "postgres"
      connBuilder.Username <- "postgres"
      connBuilder.Password <- 
        match Fake.Core.BuildServer.buildServer with
        | Travis -> ""
        | AppVeyor -> "Password12!"
        | _ -> "postgres"      
  
      let runCmd query = 
        // We wait up to 30 seconds for PostgreSQL to be initialized
        let rec runCmd' attempt = 
          try
            use conn = new Npgsql.NpgsqlConnection(connBuilder.ConnectionString)
            conn.Open()
            use cmd = new Npgsql.NpgsqlCommand(query, conn)
            cmd.ExecuteNonQuery() |> ignore 
          with e -> 
            printfn "Connection attempt %i: %A" attempt e
            Threading.Thread.Sleep 1000
            if attempt < 30 then runCmd' (attempt + 1)

        runCmd' 0
              
      let testDbName = "sqlprovider"
      printfn "Creating test database %s on connection %s" testDbName connBuilder.ConnectionString
      runCmd (sprintf "CREATE DATABASE %s" testDbName)
      connBuilder.Database <- testDbName

      (!! "src/DatabaseScripts/PostgreSQL/*.sql")
      |> Seq.map (fun file -> printfn "Running script %s on connection %s" file connBuilder.ConnectionString; file)
      |> Seq.map IO.File.ReadAllText      
      |> Seq.iter runCmd
	  *)
    ()
)

// --------------------------------------------------------------------------------------
// Set up a MS SQL Server database to run tests

let setupMssql url saPassword = 
  
    let connBuilder = System.Data.SqlClient.SqlConnectionStringBuilder()    
    connBuilder.InitialCatalog <- "master"
    connBuilder.UserID <- "sa"
    connBuilder.DataSource <- url
    connBuilder.Password <- saPassword   
          
    let runCmd query = 
      // We wait up to 30 seconds for MSSQL to be initialized
      let rec runCmd' attempt = 
        try
          use conn = new Data.SqlClient.SqlConnection(connBuilder.ConnectionString)
          conn.Open()
          use cmd = new Data.SqlClient.SqlCommand(query, conn)
          cmd.ExecuteNonQuery() |> ignore 
        with e -> 
          printfn "Connection attempt %i: %A" attempt e
          Threading.Thread.Sleep 1000
          if attempt < 30 then runCmd' (attempt + 1)

      runCmd' 0

    let runScript fileLines =            
            
      // We look for the 'GO' lines that complete the individual SQL commands
      let rec cmdGen cache (lines : string list) =
        seq {
          match cache, lines with
          | [], [] -> ()
          | cmds, [] -> yield cmds
          | cmds, l :: ls when l.Trim().ToUpper() = "GO" -> yield cmds; yield! cmdGen [] ls
          | cmds, l :: ls -> yield! cmdGen (l :: cmds) ls
        }      

      for cmd in cmdGen [] (fileLines |> Seq.toList) do
        let query = cmd |> List.rev |> String.concat "\r\n"
        runCmd query

    let testDbName = "sqlprovider"
    printfn "Creating test database %s on connection %s" testDbName connBuilder.ConnectionString
    runCmd (sprintf "CREATE DATABASE %s" testDbName)
    connBuilder.InitialCatalog <- testDbName

    (!! "src/DatabaseScripts/MSSQLServer/*.sql")
    |> Seq.map (fun file -> printfn "Running script %s on connection %s" file connBuilder.ConnectionString; file)
    |> Seq.map IO.File.ReadAllLines
    |> Seq.iter runScript
   
    (url,saPassword) |> ignore
    
Target.create "SetupMSSQL2008R2" (fun _ ->
    setupMssql "(local)\\SQL2008R2SP2" "Password12!"
)

Target.create "SetupMSSQL2017" (fun _ ->
    setupMssql "(local)\\SQL2017" "Password12!"
)


// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target.create "RunTests" (fun _ -> 

    Fake.DotNet.DotNet.test (fun p ->
        { p with
            Configuration = Fake.DotNet.DotNet.BuildConfiguration.Debug
            NoBuild = true
            NoRestore = true
            Common =
                p.Common
                |> Fake.DotNet.DotNet.Options.withAdditionalArgs [||]
            }) "SQLProvider.Tests.sln"

(*
    !! testAssemblies 
    |> Fake.DotNet.Testing.NUnit3.run (fun p ->
        { p with
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
*)
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "NuGet" (fun _ ->
    // Before release, set your API-key as instructed in the bottom of page https://www.nuget.org/account

    Fake.IO.Shell.copyDir @"temp/lib" "bin" Fake.IO.FileFilter.allFiles
    (*
    NuGet (fun p ->
        { p with
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = tags
            WorkingDir = "temp"
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        (project + ".nuspec")

    Fake.IO.Shell.cleanDir "Temp"
    Branches.tag "" release.NugetVersion
    *)
    // push manually: nuget.exe push bin\SQLProvider.1.*.nupkg -Source https://www.nuget.org/api/v2/package
    //Branches.pushTag "" "upstream" release.NugetVersion
)

Target.create "PackNuGet" (fun _ ->
    let _ = 
        Fake.DotNet.Paket.pack(fun p -> 
            { p with
                ToolType = Fake.DotNet.ToolType.CreateLocalTool()
                OutputPath = "bin"
                Version = release.NugetVersion
                ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
                Symbols = true
                })

    try 
        Branches.tag "" release.NugetVersion
    with
    | e -> 
         printfn "Git tag fail: %s" e.Message
    ()
) 

// --------------------------------------------------------------------------------------
// Generate the documentation

Target.create "GenerateHelp" (fun _ ->
    Shell.cleanDir ".fsdocs"
    let result = Fake.DotNet.DotNet.exec id "fsdocs" "build --output docs/output --input docs/content --clean"
    if not result.OK then failwith "generating reference documentation failed"
)

Target.create "WatchLocalDocs" (fun _ ->
    Shell.cleanDir ".fsdocs"
    Fake.DotNet.DotNet.exec id "fsdocs" "watch --output docs/output --input docs/content --clean --parameters fsdocs-package-project-url http://localhost:8901/" |> ignore

)

#if MONO
Target.create "SourceLink" <| fun _ -> ()
#else
//open SourceLink
Target.create "SourceLink" <| fun _ -> () (*
    let baseUrl = sprintf "%s/%s/{0}/%%var2%%" gitRaw project
    !! "src/*.fsproj"
    |> Seq.iter (fun file ->
        let proj = VsProj.LoadRelease file
        SourceLink.Index proj.CompilesNotLinked proj.OutputFilePdb __SOURCE_DIRECTORY__ baseUrl
       *)
#endif

// --------------------------------------------------------------------------------------
// Release Scripts

Target.create "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    Fake.IO.Shell.cleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    //Fake.IO.Shell.deleteDir tempDocsDir
    Fake.IO.Shell.copyRecursive "docs/output" tempDocsDir true |> Fake.Core.Trace.tracefn "%A"
    if not (System.IO.Directory.Exists tempDocsDir) then
       printfn "GH Pages not found, couldn't release."
    else
       Git.Staging.stageAll tempDocsDir
       Git.Commit.exec tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
       Branches.push tempDocsDir
)

Target.create "Release" (fun _ ->
    // push manually: nuget.exe push bin\SQLProvider.1.*.nupkg -Source https://www.nuget.org/api/v2/package
    //Branches.pushTag "" "upstream" release.NugetVersion
    ()
) 

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore


Target.create "BuildDocs" ignore

"Clean"
  ==> "AssemblyInfo"  
  // In CI mode, we setup a Postgres database before building
  =?> ("SetupPostgreSQL", not Fake.Core.BuildServer.isLocalBuild)
  // On AppVeyor, we also add a SQL Server 2008R2 one and a SQL Server 2017 for compatibility
  =?> ("SetupMSSQL2008R2", Fake.Core.BuildServer.buildServer = AppVeyor)
  =?> ("SetupMSSQL2017", Fake.Core.BuildServer.buildServer = AppVeyor)
  ==> "Build"
  ==> "BuildTests"
  ==> "RunTests"
  ==> "PackNuGet"
  ==> "CleanDocs"
  // Travis doesn't support mono+dotnet:
  =?> ("GenerateHelp", Fake.Core.BuildServer.isLocalBuild && not Fake.Core.Environment.isMono)
  ==> "All"

"Build"
  ==> "NuGet"
  
// Use this to test and run document generation in localhost:
// build -t WatchLocalDocs
"Build" 
  ==> "WatchLocalDocs"

"All"
  ==> "BuildDocs"

"All" 
#if MONO
#else
  //=?> ("SourceLink", Pdbstr.tryFind().IsSome )
#endif
  =?> ("NuGet", not(Fake.Core.Environment.hasBuildParam "onlydocs"))
  ==> "ReleaseDocs"
  ==> "Release"

"All" 
  ==> "Release"

// Change target via command-line: build -t PackNuGet
Target.runOrDefaultWithArguments "RunTests"
