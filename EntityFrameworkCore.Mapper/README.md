# EntityFrameworkCore Mapper
## Introduction
**Oasis.EntityFrameworkCore.Mapper** (referred to as **the library** in the following content) is a library that helps users to automatically map scalar and navigation properties between entity classes and other helper classes (e.g. DTOs generated with [ProtoBuf](https://developers.google.com/protocol-buffers)). It's specifically designed for the use case where:
1. Server loads some data from database to entity class instances using EntityFrameworkCore.
2. Server creates DTO class instances based on entity data, then send the DTO class from server side to client/browser side.
3. User manipulates the DTO class instances at client/browser side, add, update or delete something, then pass it back to server to update the database.
4. Server side update the entity data based on manipulated DTO and save the entities back to database.
The library helps in steps 2 and 4 to automatically map scalar and navigation properties between entity class instances and DTOs, to avoid the tedious work of hand-writing the mapping code.

Take a very simple pseudo code example below to demonstrate how it works:
Use case: a library system tracks borrowed books by borrowers.
Entitiy definitions (In BorrowRecord class apparently BorrowerId is foreign key to Borrower, and BookId is foreign key to Book, database context setup for such things is ignored here):
```C#
public sealed class Borrower
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BorrowRecord> BorrowRecords { get; set; }
}
public sealed class Book
{
    public int Id { get; set; }
    public string Name { get; set; }
    public BorrowRecord BorrowRecord { get; set; }
}
public sealed class BorrowRecord
{
    public int Id { get; set; }
    public int BorrowerId { get; set; }
    public int BookId { get; set; }
    
    public Borrower? Borrower { get; set; }
    public Book? Book { get; set; }
}
```
The DTO classes are defined as below:
```C#
public sealed class BorrowerDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BorrowRecordDTO> { get; set; }
}
public sealed class BookDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
}
public sealed class BorrowRecordDTO
{
    public int Id { get; set; }
    public int BorrowerId { get; set; }
    public int BookId { get; set; }
}
```
Assume we have the following books in database:
| Id | Name |
| --- | --- |
| 1 | Book1 |
| 2 | Book2 |
| 3 | Book3 |

The user of Id 1 has borrowed book 1 and 2, now he/she returns book 2, and borrows book 3. So when querying database and sending DTO data to client, the code is like:
```C#
var borrowerInfo = await databaseContext.Set<Borrower>().AsNoTracking().Include(b => b.BorrowRecords).FirstAsync(b => b.Id == 1);
```
In the server, upon server start up, we need to build a mapper instance and make the mapper interface available anywhere in the server code:
```C#
var factory = new MapperBuilderFactory();
var mapperBuilder = factory.Make("SomeName", defaultConfiguration);
mapperBuilder.RegisterTwoWay<Borrower, BorrowerDTO>();
var mapper = mapperBuilder.Build();
```
We can ignore the details of RegisterTwoWay, "SomeName" or defaultCongfiguration for now, meaning of the relevant code is simply, we create a mapper builder, do some configuration (like registering mapping between Borrower and BorrowerDTO), then in our server, we can start to use the mapper to map an instance of Borrower to BorrowerDTO with 2 statements:
```C#
var session = mapper.CreateMappingSession();
var borrowerDTO = session.Map<Borrower, BorrowerDTO>(entity);
```
Then the DTO instance is ready to be sent to client/browser.
So far the library works like nothing but a weakened version of AutoMapper, it's advantage will be demonstrated in later part of this example.
At client/browser side, user operates to remove borrowing record for book 2 and add in borrowing record for book 3. In the mean time, the user notices that the borrower's name is wrongly typed, so he/she decides to fix it in the same batch:
```C#
borrowerDTO.Name = "Updated Name";
var book2BorrowingRecord = borrowerDTO.BorrowRecords.Single(r => r.BookId == 2);
borrowerDTO.BorrowRecords.Remove(book2BorrowingRecord);
borrowerDTO.BorrowRecords.Add(new BorrowRecordDTO { BookId = 3 });
```
Or it can also be:
```C#
borrowerDTO.Name = "Updated Name";
var book2BorrowingRecord = borrowerDTO.BorrowRecords.Single(r => r.BookId == 2);
book2BorrowingRecord.BookId = 3;
```
Now the DTO is read to be send back to server to be processed, and the server side should simply process it this way
```C#
var session = mapper.CreateMappingToDatabaseSession(databaseContext);
var borrower = await session.MapAsync<BorrowerDTO, Borrower>(borrowerDTO)
await databaseContext.SaveChangesAsync();
```
That's it. Updating scalar properties, adding or removing entities will be automatically handled by MapAsync method of the library. Just save the changes, it will work correctly.
## User Interface
The library exposes 4 public classes/interfaces for users:
- IMapperBuilderFactory: this is the factory interface to generate a MapperBuilder to be configured, which later builds a mapper that does the work. It contains 1 Make method:
    - IMapperBuilder Make(string assemblyName, TypeConfiguration defaultConfiguration): makes the mapper builder to be configured.
        - assemblyName: this is the dynamic assembly name the mapper uses to generate static methods in, it dosn't really matter, any valid assembly name would do.
        - TypeConfiguration: this is the default configuration that will be applied to all mapped entities, it's items are:
            - identityPropertyName, name of identity property, so by default the library will assume any property named as value of this string is the id property (id is important for database records)
            - timestampPropertyName, name of timestamp property, this is supposed to be the optimistic lock column used for concurrency checking. It's OK to set it to null if most tables in the database doesn't have such concurrency check columns.
            - keepEntityOnMappingRemoved, this is a boolean item to decide when a navigation record is removed or replaced, should we keep it in database or remove it from database. By default its value is false, which represents the good database design. I'll put some more detailed information regarding this configuration item. It's highly recommended to leave it to be the default value.
- MapperBuilderFactory: this is the implementation of IMapperBuilderFactory, nothing much to explain.
- IMapperBuilder: this is the builder interface to addin configurations for mapper, it provides several methods:
    - WithFactoryMethod<TEntity>(Expression<Func<TEntity>> factoryMethod, bool throwIfRedundant = false): the library needs to create new instances of mapped entities, for [POCO](https://en.wikipedia.org/wiki/Plain_old_CLR_object#:~:text=2%20Benefits-,Etymology,sometimes%20used%20is%20plain%20old%20.)s a default constructor (parameterless) should be there, and the library counts on most entity types to have this parameterless constructor. However, in extreme situations when parameterless constructors don't exist, this interface lets users to register a factory method to build new instances of the entity type. The library doesn't allow repeatedly registering factory methods for the same TEntity type, so the second parameter will make the library throw a relevant exception when set to true, otherwise only the first registration takes effects, later repeated registrations are simply ignored.
    - IMapperBuilder WithConfiguration<TEntity>(TypeConfiguration configuration, bool throwIfRedundant = false), the library allows users to customize configurations to specific entity classes, the configuration parameter is exactly the same as that of IMapperBuilderFactory.Make method, except that it will be applied to only one entity type, and overwrites the default setting. Usage of throwIfRedundant parameter is similar to the one of IMapperBuilder.WithFactory method.
    - IMapperBuilder WithScalarConverter<TSource, TTarget>(Expression<Func<TSource?, TTarget?>> expression, bool throwIfRedundant = false), sometimes user need to use class types for scalar properties (like [ProtoBuf](https://developers.google.com/protocol-buffers) doesn't support byte array, instead uses a ByteString class instead, and unfortuantely byte array is the best type for concurrency checking property in entity framework core). To be able to map a byte array property to a ByteString class, such scalar converters needs to be defined. Usage of throwIfRedundant parameter is similar to the one of IMapperBuilder.WithFactory method.
    - IMapperBuilder Register<TSource, TTarget>(), this is the method to trigger a register of mapping between 2 types: TSource and TTarget. If users want to map an instance of TSource to an instance of TTarget, they need to register it here in the builder, or else a corresponding exception will be thrown when users try to do the mapping later. Note that the registration is recursive, like in the example in introduction, users only need to expilcitly register mapping between Borrower and BorrowerDTO, registration between navigation properties are automatically done with top level entity registered. Note that to make sure all necessary properties can be successfully mapped, the following notes must be taken into consideration:
        - For mapping scalar properties between 2 entities, like int, long, string, byte[], names of the property must be the same for source and target (e.g. X.A and Y.A, A property will be mapped, X.a and Y.A, property a and A will be considered to have different names so not mapped); Also the two properties must either be of the same type (e.g. int X.A can be mapped to int Y.A, but int X.A will not be mapped to int? Y.A), or have a scalar converter that converts from the source property type to the target property type (e.g. int X.A can be mapped to string Y.A if WithScalarConverter<int, string>(<parameters>) has been called before this registration.
    - IMapperBuilder RegisterTwoWay<TSource, TTarget>(): this is a short cut method for calling Register<A, B>(), then calling Register<B, A>(), nothing much to explain.
    - IMapper Build(): this method builds the mapper to be used. Please note that for every mapper builder instance, this method is only supposed to be called once only.
- IMapper: this is the interface for mapper, it creates 2 kinds of sessions:
    - IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext): this method creates a session that handles mapping to database.
    - IMappingSession CreateMappingSession(), this method creates a session that handles mapping when database is not involved.
- IMappingToDatabaseSession: this interface provides one asynchronous method to map to an entity that is supposed to be updated to database:
    - Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = default), source is the source object to be mapped from, includer is the Include expression for eager loading by entity framework core. If the source instance is supposed to update some existing record in database, make sure to eager-load all navigation properties with the include expression. Plus, please don't call AsNoTracking method in this includer expression, it causes problems in database updation; the library will throw an exception is users do so.
- IMappingSession: this interface provides one synchronous method to map to an entity when database is not needed:
    - TTarget Map<TSource, TTarget>(TSource source), source is the object to be mapped from, and it returns the instance of TTarget that is mapped to, nothing much to explain here.
## Highlights
1. Id and timestamp properties are considered key properties of entities, if explicitly configurated, mapping these properties doesn't need the property names to match. For example, for the following classes:
```C#
public class Entity1
{
    public int Id { get; set; }
    public byte[] TimeStamp { get; set; }
}
public class Entity2
{
    public int EntityId { get; set; }
    public byte[] ConcurrencyLock { get; set; }
}
```
If the following registration is done, then when mapping instances of these entities, id and timestamp properties will be correctly mapped:
```C#
mapperBuilder.WithConfiguration<Entity1>(new TypeConfiguration("Id", "TimeStamp")).WithConfiguration<Entity2>(new TypeConfiguration("EntityId", "ConcurrencyLock"));
```
2. About TypeConfiguration.keepEntityOnMappingRemoved, in the example in introduction section, if borrowing record of id 2 is removed from borrowerDTO, when mapping back to database, the same record shouldn't be removed from database because it doesn't make sense to keep it anymore. So value of it is set to false by default and it's not recommended to change it to true. The possibility to set it to true is kept in case the database is not well designed like below:
```C#
public sealed class Borrower
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Book> BorrowRecords { get; set; }
}
public sealed class Book
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Borrower Borrower { get; set; }
}
```
Once a book is removed from a borrower's borrow records, we don't want to delete the book, in this case keepEntityOnMappingRemoved needs to be set to be true.