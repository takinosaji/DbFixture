namespace DbFixture

open System
        
    type DbFixtureContext = {
        mutable IntegratedSecurity: bool
        mutable User: string
        mutable Password: string
        mutable Server: string
        mutable DatabaseName: string
    }
        
    type FixtureActionType =
        | DropIfExists = 1
        | CreateIfNotExists = 2
        | ReadyRollDeployment = 3
        | Sql = 4
        
    type FixtureActionDescriptor = {
        Action: DbFixtureContext -> unit
        Type: FixtureActionType
    }
    
    type IDbFixture =
        abstract Run: unit
        
