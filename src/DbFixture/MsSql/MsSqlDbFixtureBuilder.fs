namespace DbFixture.MsSql

open System
open System.Data.SqlClient
open System.Diagnostics
open DbFixture
open Dapper
open System.Collections.Generic

type MsSqlDbFixtureBuilder() =
    let [<Literal>] DbNameMaster = "master"

    member private x._fixtureContext : DbFixtureContext = {
        IntegratedSecurity = false
        User = ""
        Password = ""
        Server = ""
        DatabaseName = ""
    }
    
    member private x._fixtureActionDescriptors: List<FixtureActionDescriptor> =
        List<FixtureActionDescriptor>()
    member private x._disposeActionDescriptors: List<FixtureActionDescriptor> =
        List<FixtureActionDescriptor>()
  
    
    interface IDbFixtureBuilder with
        member x.CurrentConnectionString
            with get() =
                let connectionStringBuilder = ConnectionStringBuilder x._fixtureContext
                                              :> IConnectionStringBuilder
                connectionStringBuilder.Build()
                
            
        member x.WithIntegratedSecurity() =
           x._fixtureContext.IntegratedSecurity <- true
           upcast x
        member x.WithUser username =
            x._fixtureContext.User <- username
            upcast x
        member x.WithPassword password =
            x._fixtureContext.Password <- password
            upcast x
        member x.WithServer server =
            x._fixtureContext.Server <- server
            upcast x
        member x.WithDatabase databaseName =
            x._fixtureContext.DatabaseName <- databaseName
            upcast x
        member x.CreateIfNotExists() =
            x._fixtureActionDescriptors.Add({
                Action = fun context ->
                    let masterContext = { x._fixtureContext with DatabaseName = DbNameMaster }
                    let connectionStringBuilder = ConnectionStringBuilder masterContext
                                                  :> IConnectionStringBuilder
                    let connectionString = connectionStringBuilder.Build()
                    use connection = new SqlConnection(connectionString)
                    
                    connection.Execute(sprintf @"
                    IF DB_ID('[%s]') IS NULL
                        CREATE DATABASE [%s];
                    " context.DatabaseName context.DatabaseName) |> ignore
       
                    ()
                Type = FixtureActionType.CreateIfNotExists
            })
            upcast x
        member x.DropIfExists() =
            x._fixtureActionDescriptors.Add({
                Action = fun context ->
                    let masterContext = { x._fixtureContext with DatabaseName = DbNameMaster }
                    let connectionStringBuilder = ConnectionStringBuilder masterContext
                                                  :> IConnectionStringBuilder
                    let connectionString = connectionStringBuilder.Build()
                    use connection = new SqlConnection(connectionString)
                    
                    connection.Execute(sprintf @"
                    IF DB_ID('[%s]') IS NOT NULL
                    BEGIN
                        ALTER DATABASE [%s] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
                        DROP DATABASE [%s]
                    END
                    " context.DatabaseName context.DatabaseName context.DatabaseName) |> ignore
       
                    ()
                Type = FixtureActionType.DropIfExists
            })
            upcast x
        member x.WithReadyRollMigration deployPackagePath =
            x._fixtureActionDescriptors.Add({
                Action = fun context ->
                    let processAccessor = Process.Start(ProcessStartInfo(
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                FileName = "powershell.exe",
                                Arguments = sprintf @"
                                -NoProfile -ExecutionPolicy Unrestricted -Command ""
                                & { $DatabaseName = '%s';
                                    $DatabaseServer = '%s';
                                    %s } ""
                                " context.DatabaseName context.Server deployPackagePath
                                ))
                    
                    match processAccessor with
                        | null -> ()
                        | _ ->
                            processAccessor.WaitForExit()
                            match processAccessor.ExitCode with
                                | 0 -> ()
                                | _ ->
                                    // TODO: add error logging                       
                                    let errorOutput = processAccessor.StandardError.ReadToEnd()
                                    // TODO: add fatal logging
                                    failwith @"An error occurred during migration run.
                                        Check that path of deployment script defined correctly"
                                    ()
                            // TODO: add verbose logging
                Type = FixtureActionType.ReadyRollDeployment
            })
            upcast x
        member x.WithSql ([<ParamArray>] sqlQueries: string array) =
            x._fixtureActionDescriptors.Add({
                Action = fun context ->
                    let connectionStringBuilder = ConnectionStringBuilder context
                                                  :> IConnectionStringBuilder
                    let connectionString = connectionStringBuilder.Build()
                    use connection = new SqlConnection(connectionString)
                    for sql in sqlQueries do
                        connection.Execute sql |> ignore                   
                    ()
                Type = FixtureActionType.Sql
            })
            upcast x
        member x.WithDataSeed dataContainer =
            x._fixtureActionDescriptors.Add({
                Action = fun context ->
                    let connectionStringBuilder = ConnectionStringBuilder context
                                                  :> IConnectionStringBuilder
                    let connectionString = connectionStringBuilder.Build()
                    dataContainer.Seed connectionString          
                    ()
                Type = FixtureActionType.Sql
            })
            upcast x
        member x.DropBeforeDispose() =
            x._disposeActionDescriptors.Add({
                Action = fun context ->
                    let masterContext = { x._fixtureContext with DatabaseName = DbNameMaster }
                    let connectionStringBuilder = ConnectionStringBuilder context :> IConnectionStringBuilder
                    let connectionString = connectionStringBuilder.Build()
                    use connection = new SqlConnection(connectionString)
                    
                    connection.Execute(sprintf @"
                        ALTER DATABASE [%s]
                        SET SINGLE_USER WITH ROLLBACK IMMEDIATE
                        DROP DATABASE [%s]
                    " context.DatabaseName context.DatabaseName) |> ignore
       
                    ()
                Type = FixtureActionType.Sql
            })
            upcast x
                    
        member x.Build() =               
            upcast new MsSqlDbFixture(
                x._fixtureContext,
                MsSqlDbFixtureBuilder.SortActions x._fixtureActionDescriptors,
                MsSqlDbFixtureBuilder.SortActions x._disposeActionDescriptors
            )

    static member private SortActions (actions: List<FixtureActionDescriptor>) =
        actions.Sort(fun x y -> x.Type.CompareTo(y.Type))
        actions