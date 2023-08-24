# EF Mapper
## Introduction
**Oasis.EntityFramework.Mapper/Oasis.EntityFramework.Mapper** (referred to as **the library** in the following content) is a library that helps users to automatically map properties between different classes. Unlike AutoMapper which serves general mapping purposes, the library focus on mapping entities of EntityFramework/EntityFrameworkCore.

During implementation of a web application that relies on databases, it is inevitable for developers to deal with data objects extracted from database and [DTO](https://en.wikipedia.org/wiki/Data_transfer_object)s that are supposed to be serialized and sent to the other side of the web. These 2 kinds of objects are usually not defined to be the same classes. For example, Entity Framework uses [POCO](https://en.wikipedia.org/wiki/Plain_old_CLR_object)s for entities, while [Google ProtoBuf](https://protobuf.dev/) generates it's own class definitions for run-time efficiency during serialization and transmission advantages. Even without [Google ProtoBuf](https://protobuf.dev/), developers may define different classes from entities for DTOs to ignore some useless fields and do certain conversion before transmitting data extracted from database. **The library** is implementated for developers to handle this scenario with less coding and more accuracy.

Entities of EntityFramework/EntityFrameworkCore can be considered different from general classes in following ways:
1. An entity is considered the object side of an [Object-relation mapping](https://en.wikipedia.org/wiki/Object%E2%80%93relational_mapping).
2. An entity usually has a key property, which is mapped to the primary key column of relational database table.
3. An entity has 3 kinds of properties, scalar property that represents some value of the entity, and [navigation property](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/navigation-property) which linkes to another entity that is somehow connected to it (via a foreign key or a transparent entity in a [skip navigation](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many)).

The library focuses on use cases of mapping from/to such classes, and is integrated with EntityFramework/EntityFrameworkCore [DbContext](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext?view=efcore-7.0) for further convenience.
## Features
Main features provided by **the library** includes:
1. Basic scalar properties mapping between classes, as a trivial feature that should be provided by mappers.
2. Recursively register mapping between classes. When user registers mapping between 2 classes, navigation properties of the same property name will be automatically registered for mapping. This saves uses some coding efforts in defining class-to-class mappings.
3. Automatically search for and remove entities when mapping to entities via [DbContext](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext?view=efcore-7.0). This saves users efforts from writing tedious database operation code.
4. Identify entities by identities to guarantee uniqueness of each entity during mapping, this guarantees correctness of mapping results.
5. Some special assisting features are also provided to handle delicate use cases.
## Examples
A simple book-borrowing system is made up, and use case examples are developed based on the book-borrowing system to demonstrate how **the library** helps to save coding efforts. The following picture demonstrates the entities in the book-borrowing system.
![Book-Borrowing System Entity Graph](https://github.com/keeper013/Oasis.EFMapper/blob/main/Document/Demonstration.png)

For the 5 entities in the system:
- *Book* represents information of books, like a book can have a name, and some authors (This property is ignored to simply the example).
- *Tag* is used to categorize books, like a book can be a science fiction novel, or a dictionary; Or it may be written in English or French, and so on. A book can have many tags, and a tag may be assigned to many different books.
- *Copy* is the physical copy of a book. So there might be multiple copies of a book for different borrowers to borrow.
- *Borrower* is the person who may borrow books. One borrower can borrow multiple copies at the same time (not really demonstrated in this example), and only reserve 1 book to be borrowed.
- *Contact* is the borrower's contact information, it contains phone number and residential address in the example. This entity is used for demonstration of [one-to-one](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-one) navigation manipulation by **the library**. Value of the properties are not really important.

Sections below demonstrates usages of **the library**, all relevant code can be found in the *LibrarySample* project.
### Inserting to Database via DbContext (Basic)
```C#
// initialize mapper
var mapper = MakeDefaultMapperBuilder()
    .Register<NewTagDTO, Tag>()
    .Build();

// create new tag
await ExecuteWithNewDatabaseContext(async databaseContext =>
{
    const string TagName = "English";
    var tagDto = new NewTagDTO { Name = TagName };
    _ = await mapper.MapAsync<NewTagDTO, Tag>(tagDto, null, databaseContext);
    _ = await databaseContext.SaveChangesAsync();
    var tag = await databaseContext.Set<Tag>().FirstAsync();
    Assert.Equal(TagName, tag.Name);
});
```
This is a minimal example demonstrates basic usage of **the library**, the use case is adding a new *Tag* into the system.
- A mapper need to be defined before usage, that's what the *initialize mapper* part does.
- *MakeDefaultMapperBuilder* is a shared method defined in the test base class, it returns an instance of *IMapperBuilder* for further configuration.
- *Register<NewTagDTO, Tag>()* method configures the instance of *IMapperBuilder*, telling it to register a mapping from class *NewTagDTO* to class *Tag*. With this method called, **the library** will go through all public instance properties of class *NewTagDTO* and *Tag*, record scalar and nevigation properties that can be mapped wherever possible for later mapping process.
- *Build* method builds the instance *IMapperBuilder* into an instance of *IMapper*. After this method is called, developers can use the *IMapper* instance to map entities.
- *ExecuteWithNewDatabaseContext* is a shared method defined in the test base class, it will be used a lot in all test case code examples.
- *mapper.MapAsync<NewTagDTO, Tag>* demonstrates how the **the library** maps a [DTO](https://en.wikipedia.org/wiki/Data_transfer_object) class instance to database entities. First of all, to map to databases, the method must be asynchonized. Then, generic parameters must be provided to specify the from and to classes that the mapping should happen between, in this case it's from *NewTagDTO* to *Tag*. Among the 3 input parameters, first one is the instance of the from entity; second parameter is the [Include](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.include?view=efcore-7.0) clause of EntityFramework, which will be explain in details in use cases below when it's value is not null; third parameter is the instance of [DbContext](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext?view=efcore-7.0). The method returns the entity that is mapped to, in this use case return value of the method is ignored because it's not used. If necessary the mapped entity can be captured by a variable for further usages.
- *Assert.Equal* verifies if the new tag has been created in the database. After method *mapper.MapAsync<NewTagDTO, Tag>* is called, the entity is added to the database context, directly call DbContext.SaveChanges or DbContext.SaveChangesAsync after that will insert it into the database.

As for why the use case is inserting a new data record into the database instead of updating an existing one, the answer is that **the library** always try to match existing data records using the input data's identity property. If the input instance has an identity property and the identity property has a valid value, **the library** will try to find the matching data record in the database according to the identity property value. If found, then the existing data record will be updated according to the input instance; if not, then it's treated as an insertion use case. In this case the input class *NewTagDTO* doesn't even have an identity property, so it's treated as an insertion.

Note that **the library** is expecting every entity to have an identity property, which represents the primary key column of the corresponding data table in the database. Without this identity property the entity can't be updated by APIs of the **the library**. So far **the library** only supports a single scalar property as identity property, combined properties or class type identity property is not supported.

Check *TestCase1_MapNewEntityToDatabase.cs* for relevant examples.

### Scalar Converter and Concurrency Token
This test case demonstrates the usage of scalar converters that are used to convert one scalar type to another when mapping.

When mapping from one class to another, **the library** by default map public instance scalar properties in the 2 classes with exactly the same names and same types (Not to mention the properties must have a public getter/setter). Property name matching is case sensitive. If developers want to support mapping between property of different scalar types (e.g. from properties of type int? to properties of int, or from properties of type int to properties of long by default), a scalar converter must be defined while configuring te mapper like the examples below:
```C#
var mapper = MakeDefaultMapperBuilder()
    .WithScalarConverter<int?, int>(i => i.HasValue ? i.Value : 0)
    .WithScalarConverter<int, long>(i => i);
    // to configure/register more, continue with the fluent interface before calling Build() method.
    .Build();
```
Scalar converters can be used to define mapping from a value type to a class type as well, or from a class type to a value type, but can't be used to define mapping from one class type to another class type. One example can be found in the below example.
```C#
var mapper = MakeDefaultMapperBuilder()
    .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
    .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray());
    // to configure/register more, continue with the fluent interface before calling Build() method.
    .Build();
```
ByteArray class is the [Google ProtoBuf](https://protobuf.dev/) implementation for byte array, which is usually used as concurrency token type by EntityFramework/EntityFrameworkCore. The requirement to support converting entities to [Google ProtoBuf](https://protobuf.dev/) is the original and most important reason for **the library** to support scalar converters.
To make use of concurrency tokens, when mapping from an entity class instance to a [DTO](https://en.wikipedia.org/wiki/Data_transfer_object) class instance, concurrency token property should be included along with identity property. Then when mapping back to database with the DTO class instance to update the data record with the same identity value, concurrency token of it will be used to compare against the record stored in database, and an exception will be thrown from *MapAsync<,>* method if they don't match.

Check *TestCase2_MapEntityToDatabase_WithConcurrencyToken.cs* for relevant examples.
### Mavigaton Property Mapping
### Support for Non-Constructable-by-Default Entities
### Further Navigation Property Manipulation
### Custom Property Mapping Support
### Redudency Detection and Session
### Insert/Update Usage Restriction
## Code Structure
## Possible Improvements

Imagine a work flow with the following steps:

1. Server loads some data from database to entity class instances using EntityFramework/EntityFrameworkCore.
2. Server creates DTO class instances based on entity data, then send the DTO class from server side to client/browser side.
3. User manipulates the DTO class instances at client/browser side, add, update or delete something, then pass it back to server to update the database.
4. Server side update the entity data based on manipulated DTO and save the entities back to database.

The library helps in steps 2 and 4 to automatically map properties between entity class instances and DTOs, to avoid the tedious work of hand-writing the mapping code.

Take a very simple pseudo code example below to demonstrate how it works:
Use case: a library system tracks borrowed books by borrowers.
Entitiy definitions (In BorrowRecord class apparently BorrowerId is foreign key to Borrower, and BookId is foreign key to Book, database context setup for such things is ignored here):
```C#
public sealed class Borrower
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BorrowRecord>? BorrowRecords { get; set; }
}
public sealed class Book
{
    public int Id { get; set; }
    public string Name { get; set; }
    public BorrowRecord? BorrowRecord { get; set; }
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
    public ICollection<BorrowRecordDTO>? BorrowRecords { get; set; }
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
var mapperBuilder = factory.MakeMapperBuilder("SomeName", defaultConfiguration);
mapperBuilder.RegisterTwoWay<Borrower, BorrowerDTO>();
var mapper = mapperBuilder.Build();
```
We can ignore the details of RegisterTwoWay, "SomeName" or defaultCongfiguration for now, meaning of the relevant code is simply, we create a mapper builder, do some configuration (like registering mapping between Borrower and BorrowerDTO), then in our server, we can start to use the mapper to map an instance of Borrower to BorrowerDTO with 2 statements:
```C#
var borrowerDTO = mapper.Map<Borrower, BorrowerDTO>(entity);
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
var borrower = await mapper.MapAsync<BorrowerDTO, Borrower>(borrowerDTO, databaseContext qb => qb.Include(qb => qb.BorrowRecords))
await databaseContext.SaveChangesAsync();
```
The borrower returned is the borrower entity that will be updated into the database. Though not used in this example, returning it to user allows the user to make further updates to it (and view its content when debugging) if needed.
That's it. Updating scalar properties, adding or removing entities will be automatically handled by MapAsync method of the library. Just save the changes, it will work correctly.
## Inside View
The way this library works can be roughly explained like this:
When registerting mapping between 2 types A and B, the library will go though each public instance properties of types to be mapped (namely X and Y), and try to match the properties. All properties will be grouped under 4 categories: custom mapped property, scalar property, entity property or list property.
- When mapping instances of class X to class Y, if the library finds a property A in class X, it will try to find a property in class Y named A, the 2 properties should be of the same type, or type of X.A can be converted to type of Y.A (this conversion method must be explicitly registered to the library) so they can be mapped.
- Scalar property refer to properties of type string, byte[] or other types that are not classes.
- Entity properties refer to properties that are class types, but don't implement IEnumerable<>. When registering the mapping from type X to Y, if X.B and Y.B are matched by name and they are both categorized as entity properties, the library will try to automatically register the mapping from type of X.B to type of Y.B. So users only need to manually write the code to register the top level entities.
- Sometimes users may want to define custom mapping rules when trying to map properties that don't have the same name or type (like mapping X.A.A1 to Y.B, which will not be directly recognized as available property mapping by the library), the library provides a way of carrying out such custom property mappings.
- List properties refer to properties that are class types and implement ICollection<>, and the generic argument of ICollection<> is considered to be an entity property. When registering the mapping from type X to Y, if X.C and Y.C are matched by name and they are both categorized as list properties (To be exact, ICollection<>, IList<> and List<> are considered list types, so a property of type IList<A> will be considerd type matched to a property of type ICollection<A>, though the property types don't exactly match), similar to entity properties the library will try to automatically register the mapping between the generic argument of ICollection<> interface from X.C to that of Y.C.
    
    ❗: Custom propery mappings takes priority above all other mappings. Any property in the target type that is mapped by custom property mapping will not be mapped again by any other mappings, even if a property with the same name and type exists in the source type.
    
    ❗: Do check property names and types between the types that are to be mapped, matching of property names will be case sensitive, and only exactly same property types will be considered directly matching, the library will **silently** ignore unmatched properties when making matches. For example a property of name "**Test**" will not be matched to another property named "**test**", a property of type **int** will not be matched to an other property of type **int?**, and a property of type **long** will not be matched to a property of type **ulong**.
    
    ❗: Note that when mapping properties, getters and setters are important. To Map X.A to Y.A, X.A must have a getter, and Y.A must have both a getter and a setter (entity list property doesn't needs to have setters if the class guarantees that the list will be initialized upon construction, this exception is made due to implementation of RepeatedField of [ProtoBuf](https://developers.google.com/protocol-buffers)), or else the library will not consider X.A and Y.A a valid match, even if property name and type are successfully matched.
    
    ❗: Note that each type can be only be registered as a scalar type, entity type or entity list type. If a type is registered to be convertable to a scalar type, then this type will not be accepted to be registered as an entity type anymore. Vice-Versa, if a type is registered to be an entity type, it can't be registered to be convertable to a scalar type. A type implements IEnumerable<> won't be accepted as an entity type, and only types implements ICollection<> may be recognized as an entity list type.

The library will generate a dynamic assembly and some static methods when users register mapping between the types. Those generated static methods will be used later when users build the IMapper interface and use it to map properties.
## User Interface
The library exposes the following public classes/interfaces for users:
- IMapperBuilderFactory: this is the factory interface to generate a MapperBuilder to be configured, which later builds a mapper that does the work. It contains 1 Make method:
    - IMapperBuilder MakeMapperBuilder(string assemblyName, TypeConfiguration? defaultConfiguration): makes the mapper builder to be configured.
        - assemblyName: this is the dynamic assembly name the mapper uses to generate static methods in, it dosn't really matter, any valid assembly name would do.
        - TypeConfiguration: this is the default configuration that will be applied to all mapped entities, it's items are:
            - identityPropertyName, name of identity property, so by default the library will assume any property named as value of this string is the id property (id is important for database records), defaulted to "Id" if the defaultConfiguration parameter is null.
            - concurrencyTokenPropertyName, name of concurrency token property, this is supposed to be the optimistic lock column used for concurrency checking. It's OK to set it to null if most tables in the database doesn't have such concurrency check columns, defaulted to null.
            - keepEntityOnMappingRemoved, this is a boolean item to decide when a navigation record is removed or replaced, should we keep it in database or remove it from database. By default its value is false, which represents the good database design. Some more detailed information regarding this configuration item can be found in later sections. It's highly recommended to leave it to be the default value.
    - ICustomPropertyMapperBuilder<TSource, TTarget> MakeCustomPropertyMapperBuilder<TSource, TTarget>(): makes a custom property mapper builder to customize certain property mappings between TSource and TTarget.
        - TSource: source type
        - TTarget: target type
- MapperBuilderFactory: this is the implementation of IMapperBuilderFactory, nothing much to explain.
- ICustomPropertyMapperBuilder<TSource, TTarget>: this is the interface for custom property mapper.
    - ICustomPropertyMapperBuilder<TSource, TTarget> MapProperty<TProperty>(Expression<Func<TTarget, TProperty>> setter, Expression<Func<TSource, TProperty>> value): this method takes in 2 expressions to assign value calculated from source type to a target type property. Examples of the function would be: x.MapProperty(target => target.PropertyA, source => source.B.X + source.C), which means when mapping source to target, value of propertyA of target will be the sum of property X of property B of source and property C of source.
    - CustomPropertyMapper<TSource, TTarget> Build(): this function builds the builder into the parameter type to be accepted by IMapperBuilder.Register and IMapperBuilder.RegisterTwoWays.
- IMapperBuilder: this is the builder interface to addin configurations for mapper, it provides several methods:
    - WithFactoryMethod<TEntity>(Expression<Func<TEntity>> factoryMethod, bool throwIfRedundant = false): the library needs to create new instances of mapped entities, for [POCO](https://en.wikipedia.org/wiki/Plain_old_CLR_object#:~:text=2%20Benefits-,Etymology,sometimes%20used%20is%20plain%20old%20.)s a default constructor (parameterless) should be there, and the library counts on most entity types to have this parameterless constructor. However, in extreme situations when parameterless constructors don't exist, this interface lets users to register a factory method to build new instances of the entity type. The library doesn't allow repeatedly registering factory methods for the same TEntity type, so the second parameter will make the library throw a relevant exception when set to true, otherwise only the first registration takes effects, later repeated registrations are simply ignored.
    - IMapperBuilder WithFactoryMethod<TList, TItem>(Expression<Func<TList>> factoryMethod, bool throwIfRedundant = false): this method registers a factory method for user defined collection types in case they are of interface types or don't have a default constructor that the library doesn't know how to instantiate such list properties during mapping process. Like [ProtoBuf](https://developers.google.com/protocol-buffers) uses RepeatedField class for collection properties. In case the property is not initialized (This situation doesn't really fit [ProtoBuf](https://developers.google.com/protocol-buffers) because it does initialize RepeatedField properties when the message is constructed) when some entities need to be filled in it, the library needs to initialize the customized collection type. The library will automatically initialize a List<T> for ICollection<T>, IList<T> and List<T> types, or try to call the default parameterless constructor of the collection type. If the collection type doesn't fit in the 2 situations, then user must provide a factory method for this collection type, or else a corresponding exception will be thrown at run time.
    - IMapperBuilder WithConfiguration<TEntity>(TypeConfiguration configuration, bool throwIfRedundant = false), the library allows users to customize configurations to specific entity classes, the configuration parameter is exactly the same as that of IMapperBuilderFactory.Make method, except that it will be applied to only one entity type, and overwrites the default setting. Usage of throwIfRedundant parameter is similar to the one of IMapperBuilder.WithFactory method.
    - IMapperBuilder WithScalarConverter<TSource, TTarget>(Expression<Func<TSource?, TTarget?>> expression, bool throwIfRedundant = false), sometimes user need to use class types for scalar properties (like [ProtoBuf](https://developers.google.com/protocol-buffers) doesn't support byte array, instead uses a ByteString class instead, and unfortuantely byte array is the best type for concurrency checking property in entity framework core). To be able to map a byte array property to a ByteString class, such scalar converters needs to be defined. Usage of throwIfRedundant parameter is similar to the one of IMapperBuilder.WithFactory method.
    - IMapperBuilder Register<TSource, TTarget>(ICustomPropertyMapper<TSource, TTarget>? customPropertyMapper = null), this is the method to trigger a register of mapping between 2 types: TSource and TTarget. If users want to map an instance of TSource to an instance of TTarget, they need to register it here in the builder, or else a corresponding exception will be thrown when users try to do the mapping later. Note that the registration is recursive, like in the example in introduction, users only need to expilcitly register mapping between Borrower and BorrowerDTO, registration between navigation properties are automatically done with top level entity registered. Note that to make sure all necessary properties can be successfully mapped, the following notes must be taken into consideration:
        - The parameter customPropertyMapper is the customPropertyMapper which contains all custom property mapping logic when mapping from class TSource to TTarget. Instance of this interface is generated by ICustomPropertyMapperBuilder<TSource, TTarget>.Build method. Note that this parameter will not work in resursive mapping case. For example if class A1 contains property B of class type B1, class A2 contains property B of class type B2, if a mapping between A1 and A2 is registered with IMapperBuilder.Register method, a mapping between class B1 and B2 will be automatically done during the registration, which is called recursive mapping. It's apparent that an instance of ICustomPropertyMapperBuilder<TSource, TTarget> won't work for the mapping from B1 and B2, so it will only work when registering mapping from A1 to A2. But pay attention, in this case, if it is intended to register a mapping between B1 and B2 **with** a custom property mapper, the registration must happen before registering the mapping between A1 and A2 so it happens before registering mapping beween B1 and B2 is triggered recursively without a custom property mapper, or else a corresponding exception will be thrown at registering phase to remind the developer to adjust the registering order.
        - For mapping scalar properties between 2 entities, like int, long, string, byte[], names of the property must be the same for source and target (e.g.: X.A and Y.A, A property will be mapped, X.a and Y.A, property a and A will be considered to have different names so not mapped); Also the two properties must either be of the same type (e.g.: int X.A can be mapped to int Y.A, but int X.A will not be mapped to int? Y.A), or have a scalar converter that converts from the source property type to the target property type (e.g.: int X.A can be mapped to string Y.A if WithScalarConverter<int, string>(<parameters>) has been called before this registration).
    - IMapperBuilder RegisterTwoWay<TSource, TTarget>(ICustomPropertyMapper<TSource, TTarget>? customPropertyMapperSourceToTarget = null, ICustomPropertyMapper<TTarget, TSource>? customPropertyMapperTargetToSource = null): this is a short cut method for calling Register<A, B>(ICustomPropertyMapper<A, B>?), then calling Register<B, A>(ICustomPropertyMapper<B, A>?), nothing much to explain.
    - IMapper Build(): this method builds the mapper to be used. Please note that for every mapper builder instance, this method is only supposed to be called once only.
- IMapper: this is the interface for mapper, it can be directly used for mapping, or creates 2 kinds of sessions:
    - TTarget Map<TSource, TTarget>(TSource source): this method directly maps one entity type to the other, no database is involved in this mapping, it's a short cut for IMappingSession.Map method if there is only 1 entity to map.
    - Task<TTarget> MapAsync<TSource, TTarget>(TSource source, DbContext databaseContext, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = default, MapToDatabaseType mappingType = MapToDatabaseType.Upsert), this method directly maps one entity to the other in database, it will try to load the entity first from the database if the entity already exists (if id is not empty), it's a short cut for IMappingToDatabaseSession.MapAsync methode if there is only 1 entity to map. Please refer to the same method of IMappingtoDatabaseSession for input parameter details.
    - IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext): this method creates a session that handles mapping to database.
    - IMappingSession CreateMappingSession(), this method creates a session that handles mapping when database is not involved.
- IMappingToDatabaseSession: this interface provides one asynchronous method to map to an entity that is supposed to be updated to database:
    - Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = default, MapToDatabaseType mappingType = MapToDatabaseType.Upsert), source is the source object to be mapped from, includer is the Include expression for eager loading by entity framework core. If the source instance is supposed to update some existing record in database, please **make sure** to eager-load all navigation properties with the include expression. Plus, please don't call AsNoTracking method in this includer expression, it causes problems in database updation; the library will throw an exception is users do so. Parameter mappingType is to state whether this mapping should do insert only, updsate only or upsert. Relevant exceptions will be thrown according to this parameter under certain conditions (E.g. entity id is null for update only, the target entity doesn't exist in database for update only, the entity with the given id already exists for insert only).
- IMappingSession: this interface provides one synchronous method to map to an entity when database is not needed:
    - TTarget Map<TSource, TTarget>(TSource source), source is the object to be mapped from, and it returns the instance of TTarget that is mapped to, nothing much to explain here.
## Highlights
1. When calling MapAsync to map entities to database:
    - If the entity doesn't have an Id property, the entity will always be mapped as a new entity to be inserted into database.
    - If the entity has an Id property, but the value is default value (e.g. 0 for int, null for int?), the entity will always be mapped as new entity to be inserted into database.
    - If the entity has an Id property, and the Id property's value is not default value, but record of that id value doesn't exist in the correponding database table, the entity will be mapped as new entity to be inserted into database, and id of the target entity will be set to the same value of source entity. The target entity will be successfully inserted into database, but inserted id value may differ. With entity framework core, the inserted id value will be the same as source entity id value. With entity framework 6.0, id value will be automatically generated if stated for the id value, instead of following what was set for source entity.
    - If the entity has an Id property, and the Id property's value is not default value, and the record of tha tid can be found in the corresponding database table, the entity will be mapped as existing entity to update the existing data record.
2. Id and concurrency token properties are considered key properties of entities, if explicitly configurated, mapping these properties doesn't need the property names to match. For example, for the following classes:
```C#
public class Entity1
{
    public int Id { get; set; }
    public byte[] ConcurrencyToken { get; set; }
}
public class Entity2
{
    public int EntityId { get; set; }
    public byte[] ConcurrencyLock { get; set; }
}
```
If the following registration is done, then when mapping instances of these entities, id and concurrency token properties will be correctly mapped:
```C#
mapperBuilder.WithConfiguration<Entity1>(new TypeConfiguration("Id", "ConcurrencyToken")).WithConfiguration<Entity2>(new TypeConfiguration("EntityId", "ConcurrencyLock"));
```
3. About TypeConfiguration.keepEntityOnMappingRemoved, in the example in introduction section, if borrowing record of id 2 is removed from borrowerDTO, when mapping back to database, the same record shouldn't be removed from database because it doesn't make sense to keep it anymore. So value of it is set to false by default and it's not recommended to change it to true. The possibility to set it to true is kept in case the database is not well designed like below:
```C#
public sealed class Borrower
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Book> BorrowedBooks { get; set; }
}
public sealed class Book
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int BorrowerId { get; set; }
    public Borrower Borrower { get; set; }
}
```
Once a book is removed from a borrower's borrowed books, we don't want to delete the book, in this case keepEntityOnMappingRemoved needs to be set to be true.
4. Please note that when mapping reference type properties, a **shallow** copy will be performed.
5. What are sessions used for? Why can't mapping be carried out directy by IMapper interface?
    In a nutshell, sessions keep track of newly added entities (entities with empty ids will be considered entities that will be inserted as new data record into database) avoid them gets duplicated. To use the example in introduction section to explain: let's say the library now requires book borrowing records to be monitored by some book keeper (maybe the book keepers will call the borrowers to remind them a few days earlier before the books' due date), and the book keeper is optional to borrow record (to allow the system to assign a borrow record to a book keeper after the book is borrowed), then BorrowRecord needs to be redefined:
```C#
public sealed class BorrowRecord
{
    public int Id { get; set; }
    public int BorrowerId { get; set; }
    public int BookId { get; set; }
    public int BookKeeperId { get; set; }
    
    public Borrower? Borrower { get; set; }
    public BookKeeper? BookKeeper { get; set; }
    public Book? Book { get; set; }
}
public sealed class BookKeeper
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BorrowRecord>? BorrowRecords { get; set; }
}
```
When a borrower borrows a book, if we want to assign the borrow record to a book keeper at the same time, the following code will work correctly.
```C#
var borrowRecord = new BorrowRecordDTO { BookId = borrowedBookId };

// to make it short and clear, assume the all DTOs have been preloaded and mapped from database.
borrowerDTO.BorrowRecords.Add(borrowRecord);
bookKeeperDTO.BorrowRecords.Add(borrowRecord);
    
var session = mapper.CreateMappingToDatabaseSession(databaseContext);
await session.MapAsync<BorrowerDTO, borrower>(borrowerDTO, qb => qb.Include(qb => qb.BorrowRecords));
await session.MapAsync<BookKeeperDTO, BookKeeper>(bookKeeperDTO, qb => qb.Include(qb => qb.BorrowRecords));
await databaseContext.SaveChanges();
```
In the code, the new record borrowRecord was supposed to be added to both a book keeper's and a borrower's borrow records. Mapping the book keeper and the borrower using the same session guarantees that this new borrow record doesn't get inserted into database twice, that when mapping borrowerDTO and bookKeeperDTO, the same borrow record entity will be used by databaseContext to assign borrower id and book keeper id.
If it is done the other way around as demonstrated below then the result will be incorrect, 2 different borrow records will be inserted into database (if borrower id is compulsory to borrow record then an exception will be thrown).
```C#
// without creating sessions, each MapAsync call is defaulted to have its own session.
await mapper.MapAsync<BorrowerDTO, borrower>(borrowerDTO, databaseContext, qb => qb.Include(qb => qb.BorrowRecords));
await mapper.MapAsync<BookKeeperDTO, BookKeeper>(bookKeeperDTO, databaseContext, qb => qb.Include(qb => qb.BorrowRecords));
await databaseContext.SaveChanges();
```
Of course the best way to do in this case is to simply set borrower id and book keeper id to the borrow record dto and only map the borrow record dto back to database to be saved. This example does it the stupid way to demonstrate the difference of using a single mapping session and different mapping sessions. In a rare case if users want to re-use the same DTO to inject multiple records to the database, they may consider using multiple mapping sessions to achieve it.
## Restriction
1. The library assumes that any entity could only have 1 property as Id property, multiple properties combined id is not supported.
2. The library requires any entity that will get updated into database to have an Id property, which means every table in the database, as long as it will be mapped using the library, need to have 1 and only 1 column for Id.
3. The library requires each property to have 1 or 0 column for optimistic lock.
## Feedback
There there be any questions or suggestions regarding the library, please send an email to keeper013@gmail.com for inquiry.
When submitting bugs, it's preferred to submit a C# code file with a unit test to easily reproduce the bug.
