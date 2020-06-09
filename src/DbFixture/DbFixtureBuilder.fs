namespace DbFixture

    open System.Data.SqlClient
    
    type IConnectionStringBuilder =
        abstract Build: unit -> string
            
    type ConnectionStringBuilder(context: DbFixtureContext) =
        interface IConnectionStringBuilder with
            member x.Build() =
                let builder = SqlConnectionStringBuilder(
                    IntegratedSecurity = context.IntegratedSecurity,
                    DataSource = context.Server,
                    InitialCatalog = context.DatabaseName,
                    Pooling = false
                )       
                              
                if not (System.String.IsNullOrEmpty(context.User))
                && not (System.String.IsNullOrEmpty(context.Password)) then
                    builder.UserID <- context.User
                    builder.Password <- context.Password
                
                builder.ConnectionString
                    
    type IConnectionStringHolder =
        abstract CurrentConnectionString: string with get
                        
    type IDbFixtureBuilder =
        inherit IConnectionStringHolder
        abstract WithIntegratedSecurity: unit -> IDbFixtureBuilder
        abstract WithUser: string -> IDbFixtureBuilder
        abstract WithPassword: string -> IDbFixtureBuilder
        abstract WithServer: string -> IDbFixtureBuilder
        abstract WithDatabase: string -> IDbFixtureBuilder
        abstract CreateIfNotExists: unit ->IDbFixtureBuilder
        abstract DropIfExists: unit -> IDbFixtureBuilder
        abstract WithReadyRollMigration: string -> IDbFixtureBuilder
        abstract WithSql: string array -> IDbFixtureBuilder
        abstract WithDataSeed: IDataSeed -> IDbFixtureBuilder
        abstract DropBeforeDispose: unit -> IDbFixtureBuilder
        abstract Build: unit -> IDbFixture
        