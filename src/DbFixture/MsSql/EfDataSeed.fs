module DbFixture.EF

    open System
    open System.Collections.Generic
    open System.Data.Entity
    open System.Data.Entity.Validation

    type EfDataSeed(
                       dbContextFactory: IDbContextFactory
                   ) =
        member private x._entities = List<Object>()
        
        interface IDataSeed with
            member x.With<'a when 'a :not struct>([<ParamArray>] entities: 'a array) =
                for entity in entities do
                    x._entities.Add entity
                upcast x
                
            member x.Seed connectionString =
                use dbContext = dbContextFactory.GetDbContext(connectionString)
                for entity in x._entities do
                    let entry = dbContext.Entry(entity)
                    entry.State <- EntityState.Added
                    try
                        dbContext.SaveChanges() |> ignore
                    with :? DbEntityValidationException as e ->
                        for entityValidationError in e.EntityValidationErrors do
                            Console.Write(sprintf "%O errors:" entityValidationError.Entry.Entity)
                            for validationError in entityValidationError.ValidationErrors do
                                Console.Write(sprintf "%s - %s" validationError.PropertyName validationError.ErrorMessage)
                            
