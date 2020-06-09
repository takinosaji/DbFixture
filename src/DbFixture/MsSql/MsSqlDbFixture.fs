namespace DbFixture.MsSql

    open System
    open DbFixture
    open System.Collections.Generic
        
    type MsSqlDbFixture(
                       context: DbFixtureContext,
                       fixtureActionDescriptors: List<FixtureActionDescriptor>,
                       disposeActionDescriptors: List<FixtureActionDescriptor>
                       ) =
        interface IDbFixture with
            member x.Run =
                for descriptor in fixtureActionDescriptors do
                    descriptor.Action context
                    
        interface IDisposable with
            member x.Dispose() =
                for descriptor in disposeActionDescriptors do
                    descriptor.Action context
                        