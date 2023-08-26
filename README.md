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
Scalar converters can be used to define mapping from a value type to a class type as well, or from a class type to a value type, but can't be used to define mapping from one class type to another class type. One example can be found below.
```C#
// initialize mapper
var mapper = MakeDefaultMapperBuilder()
    .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
    .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
    .Register<NewBookDTO, Book>()
    .RegisterTwoWay<Book, UpdateBookDTO>()
    .Build();

// create new book
const string BookName = "Book 1";
Book book = null!;
await ExecuteWithNewDatabaseContext(async databaseContext =>
{
    var bookDto = new NewBookDTO { Name = BookName };
    _ = await mapper.MapAsync<NewBookDTO, Book>(bookDto, null, databaseContext);
    _ = await databaseContext.SaveChangesAsync();
    book = await databaseContext.Set<Book>().FirstAsync();
    Assert.Equal(BookName, book.Name);
});

// update existint book dto
const string UpdatedBookName = "Updated Book 1";
var updateBookDto = mapper.Map<Book, UpdateBookDTO>(book);
Assert.NotNull(updateBookDto.ConcurrencyToken);
Assert.NotEmpty(updateBookDto.ConcurrencyToken);
updateBookDto.Name = UpdatedBookName;

await ExecuteWithNewDatabaseContext(async databaseContext =>
{
    _ = await mapper.MapAsync<UpdateBookDTO, Book>(updateBookDto, null, databaseContext);
    _ = await databaseContext.SaveChangesAsync();
    book = await databaseContext.Set<Book>().FirstAsync();
    Assert.Equal(UpdatedBookName, book.Name);
});
```
*ByteString* class is the Google ProtoBuf implementation for byte array, which is usually used as concurrency token type by EntityFramework/EntityFrameworkCore. The requirement to support converting entities to Google ProtoBuf is the original and most important reason for **the library** to support scalar converters.
In the sample code above:
- Book entity has a concurrency token property of type *byte[]*, so the UpdateBookDTO generated by Google ProtoBuf has concurrency token property of type *ByteString*, hence scalar converters between the 2 types are required.
- *RegisterTwoWay<A, B>* simply means Register<A, B> then Register<B, A>, so instances of 2 different classes can be mapped in either direction.
- NewBookDTO doesn't have any identity or concurrency token properties, which makes sense because identity of Book entity is configured to be generated upon insertion, and concurrency token doesn't make any sense before an entity gets persisted into the database.
- Note that unless specially configurated, **the library** won't map properties if the names don't match. When mapping NewBookDTO to Book, identity and concurrency token of Book will be left to their default value (in this case 0 or null), then entity framework detects the empty identity property and treats the mapped book entity as an insertion case.
- *mapper.Map* method is an example of mapping database entity instances to DTO instances, its synchronize, and doesn't need include and DbContext input parameters. This method can serve as trival mapping from one class to another use cases, and will be use a lot in the following test codes.
- For DTO classes, identity and concurrency token properties are only required if it is supposed to be used to update existing data in the database. When updating existing data records in database with the DTO class instances, concurrency token of it will be used to compare against the record stored in database. As the way optimistic locking and concurrency token should work, an exception will be thrown from *MapAsync<,>* method if the concurrency tokens don't match.

Check *TestCase2_MapEntityToDatabase_WithConcurrencyToken.cs* for relevant examples.
### Mavigaton Property Mapping
### Support for Non-Constructable-by-Default Entities
### Further Navigation Property Manipulation
### Custom Property Mapping Support
### Redudency Detection and Session
### Insert/Update Usage Restriction
## Code Structure
## Possible Improvements
## Feedback
There there be any questions or suggestions regarding the library, please send an email to keeper013@gmail.com for inquiry.
When submitting bugs, it's preferred to submit a C# code file with a unit test to easily reproduce the bug.
