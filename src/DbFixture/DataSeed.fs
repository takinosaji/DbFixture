namespace DbFixture

    open System.Data.Entity
    
    type IDataSeed =
        abstract With<'a> : 'a array -> IDataSeed
            when 'a :not struct
        abstract Seed: string -> unit
              
    type IDbContextFactory =
        abstract GetDbContext: string -> DbContext