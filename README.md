# EF Mapper
## Introduction
**Oasis.EntityFramework.Mapper/Oasis.EntityFramework.Mapper** (referred to as **the library** in the following content) is a library that helps users (referred to as "developers" in the following document, as users of this libraries are developers) to automatically map properties between different classes. Unlike AutoMapper which serves general mapping purposes, the library focus on mapping entities of EntityFramework/EntityFrameworkCore.

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

Sections below demonstrates usages of **the library**, all relevant code can be found in the *LibrarySample* project. Considering length of the descriptions, it is recommended to download the whole project and directly read the test code in LibrarySample folder first, and come back to the descriptions below whenever the code itself isn't descriptive enough.
### TestCase1_MapNewEntityToDatabase
This test case demonstrates the basic usage of **the library** on how to insert data into database with it.
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

### TestCase2_MapEntityToDatabase_WithConcurrencyToken
This test case shows usage of scalar converters, concurrency token and the way to update scalar properties using **the library**.

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
- Note that unless specially configured, **the library** won't map properties if the names don't match. When mapping NewBookDTO to Book, identity and concurrency token of Book will be left to their default value (in this case 0 or null), then entity framework detects the empty identity property and treats the mapped book entity as an insertion case.
- *mapper.Map* method is an example of mapping database entity instances to DTO instances, its synchronize, and doesn't need include and DbContext input parameters. This method can serve as trival mapping from one class to another use cases, and will be use a lot in the following test codes.
- For DTO classes, identity and concurrency token properties are only required if it is supposed to be used to update existing data in the database. When updating existing data records in database with the DTO class instances, concurrency token of it will be used to compare against the record stored in database. As the way optimistic locking and concurrency token should work, an exception will be thrown from *MapAsync<,>* method if the concurrency tokens don't match.
- I haven't found a way for EF6 to work very well with SQLite having concurrency token of type *byte[]*, so we use type *long* instead.
### TestCase3_MapNavigationProperties_WithUnmapped
This test case demonstrates basics for updating navigation properties using the library.
#### Identity and Concurrency Token Properties configuration
In this section, the example code will do mapping for *Borrower* entity. Definition of this entity is different than those for *Tag* and *Book*. Below are the definitions of the 3 entities:
```C#
public sealed class Borrower : IEntityBaseWithConcurrencyToken
{
    public string IdentityNumber { get; set; } = null!;
    public long ConcurrencyToken { get; set; }
    public string Name { get; set; } = null!;
    public Contact Contact { get; set; } = null!;
    public Copy Reserved { get; set; } = null!;
    public List<Copy> Borrowed { get; set; } = new List<Copy>();
}
public sealed class Book : IEntityBaseWithId, IEntityBaseWithConcurrencyToken
{
    public int Id { get; set; }
    public long ConcurrencyToken { get; set; }
    public string Name { get; set; } = null!;
    public List<Copy> Copies { get; set; } = new List<Copy>();
    public List<Tag> Tags { get; set; } = new List<Tag>();
}
public sealed class Tag : IEntityBaseWithId
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<Book> Books { get; set; } = new List<Book>();
}
```
Focus on the 3 definitions are identity and concurrency token properties. For *Tag* class, the identity property is named "Id" of type *int*, and it doesn't have a concurrency token property; for *Book* class, the identity property is named "Id" of type integer, and the concurrency token property if named ConcurrencyToken of type *byte[]*; for *Borrower* class, the identity property is named "IdentityNumber" of *string* type, and the concurrency token property if named ConcurrencyToken of type *byte[]*. So the the question is obvious, how to tell which property is for identity or concurrency token?
**The library** allows developers to define the default and per-class names for these properties. Take a look at the definition of *MakeDefaultMapperBuilder* method:
```C#
protected static IMapperBuilder MakeDefaultMapperBuilder(string[]? excludedProperties = null)
{
    return new MapperBuilderFactory()
        .Configure()
            .SetKeyPropertyNames(nameof(IEntityBaseWithId.Id), nameof(IEntityBaseWithConcurrencyToken.ConcurrencyToken))
            .ExcludedPropertiesByName(excludedProperties)
            .Finish()
        .MakeMapperBuilder();
}
```
The method *ExcludedPropertiesByName* will described in later part. Except that, points of the sample code above include:
- *MapperBuilderFactory* class is literally entry of **The library**. Calling any API provided by **the library** only happens a new instance of this class is created.
- The *Configure* and *Finally* pattern is the most used fluent API configuration pattern of **the library** for specifially configuring *MapperBuilderFactory* class, mapping of a class, and mapping between 2 classes. When *Configure* method is called, configuration enters the mode of configuring a certain sub category, and *Finish* is the method to call when configuration of the sub category is done, and developer want to go back to root configuration to continue configuring other sub categories or register mappings.
- *SetKeyPropertyNames* method sets the default name of identity and concurrency token. So whenever developer registers a mapping between any 2 classes, **The library** by default try to find properties with the 2 names passed as input parameters to this method as identity and concurrency token properties. This works for *Tag* and *Book* entities.
- Note that *Tag* class doesn't really have a concurrency token property. In this case, **the library** considers this class as being "without a concurrency token", it's still possible to update *Tag* entities using **the library** as the normal way, just no optimistic lock feature can be applied to the entity, the update will always go through.
- For classes that doesn't have an identity property, **the library** can only be used to insert new records into database for them, updating such entities will not be possible.
- For *Borrower* class, its identity property is with a different name from default, that's the reason **the library** introduced class level configuration for identity and concurrency token properties configuration. The example is as below:
```C#
var mapper = MakeDefaultMapperBuilder()
    .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
    .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
    .Configure<Borrower>()
        .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
        .Finish()
    .Configure<NewBorrowerDTO>()
        .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
        .Finish()
    .Configure<UpdateBorrowerDTO>()
        .SetKeyPropertyNames(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
        .Finish()
    .Register<NewBorrowerDTO, Borrower>()
    .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
.Build();
```
To continue the points for configuration of identity and concurrency token properties:
- *Borrower* class has concurrency token of type *byte[]*, that's the reason we need the scalar converters. 
- *Configure<Borrower>* method makes configuration of IMapperBuilder into configuration mode for class *Borrower*, as the *Configure* and *Finish* mode metioned previously.
- Configuration of identity and concurrency token name on specific classes override the default configuration.
- **The library** provides APIs for developers to configure identity and concurrency token properties separately, namely *SetIdentityPropertyName* and *SetConcurrencyTokenPropertyName*, in case they don't need to be set together.
- It is quite obvious that if an entity has a different ientity or concurrency token property name from default, itself and all relevant DTO classes need to be specifically configured. Having all entities using the same identity and concurrency property name can really help saving some configuration effort.
- More configuration options will be described in later sections.
#### Recursive Registration
It's clear from definition of the 3 classes that they have navigation properties. Take *Borrower* for example, it has a "Contact" property of type *Contact*, a "Reserved" property of type *Copy*, and a "Borrowed" property of type *List<Copy>*; while *UpdateBorrowerDTO* class has a "Contact" property of type *UpdateContactDTO*, a "Reserved" property of type *CopyReferenceDTO*, and a "Borrowed" property of type *pbc::RepeatedField<CopyReferenceDTO>*. When registering the mapping from *Borrower* to *UpdateBorrowerDTO*, the following things will happen.
- **The library** will find out that both classes have properties named "Contact", "Reserved" and "Borrowed", and find out that these are not scalar properties. Then it will automatically try to register mapping between types of such entities so mapping for such navigation properties will automatically happen when the root classes are being mapped. The registration is recursive, which is designed for convenience of developers that they don't really need to manually register all the navigation properties of each entities, as long as the names match and types are valid, **the library** will do this automatically.
- There will be no need to worry about the recursive registration becomes an infinite loop caused by loop dependency formed by entities, a mechanism exists to detect such loop dependencies and break out from it once detected.
- For one-to-one relationships, like "Contact" property when mapping from *Borrower* to *UpdateBorrowerDTO*, **the library** will find out that property type of it for *Borrower* is *Contact*, and that of *UpdateBorrowerDTO* is *UpdateContactDTO*. Both types are classes, so it will go on to register the mapping from *Contact* to *UpdateContactDTO*.
- For one-to-many or many-to-many relationships, like "Borrowed" property when mapping from *Borrower* to *UpdateBorrowerDTO*, **the library** will find out that property type of it for *Borrower* is *List<Copy>*, and that of *UpdateBorrowerDTO* is *pbc::RepeatedField<CopyReferenceDTO>*. Both types are class collection types, so it will go on to register the mapping from *Copy* to *CopyReferenceDTO*.
- Definition of "class collection type" includes: ICollection<T> where T : class, IList<T> where T : class, List<T> where T : class, or a type that inherit from/implements any of the 3 (which fits type pbc::RepeatedField<CopyReferenceDTO>).
- *CopyReferenceDTO* is designed to have an identity property only for a purpose to avoid updating *Copy* instances from borrowers, because detailed information of borrowers and copies are not business-wise relevant and should be managed separately. If developers replace *CopyReferenceDTO* with *UpdateCopyDTO* in UpdateBorrowerDTO*, they totally can load a borrower with borrowed books, map it to an *UpdateBorrowerDTO*, update the borrower's address and number of a copy, then map it back to the database, **the library** will really update the borrower and the copy as the DTOs are updated. But technical possibility doesn't necessarily make sense in business, hence the trick in designing entity DTOs with only an identity property is adopted to aviod unintended data modifications.
#### Recursive Mapping
As registration process, mapping process will be recursive, too. Similarly a mechanism to prevent infinite loop is implemented.
```C#
private async Task AddAndUpateBorrower(IMapper mapper, string address1, string? assertAddress1, string? assertAddress2, string address3, string? assertAddress3)
{
    // create new book
    const string BorrowerName = "Borrower 1";
    
    Borrower borrower = null!;
    await ExecuteWithNewDatabaseContext(async databaseContext =>
    {
        var borrowerDto = new NewBorrowerDTO { IdentityNumber = "Identity1", Name = BorrowerName, Contact = new NewContactDTO { PhoneNumber = "12345678", Address = address1 } };
        _ = await mapper.MapAsync<NewBorrowerDTO, Borrower>(borrowerDto, null, databaseContext);
        _ = await databaseContext.SaveChangesAsync();
        borrower = await databaseContext.Set<Borrower>().Include(b => b.Contact).FirstAsync();
        Assert.Equal(BorrowerName, borrower.Name);
        Assert.Equal(assertAddress1, borrower.Contact.Address);
    });

    // update existing book dto
    const string UpdatedBorrowerName = "Updated Borrower 1";
    var updateBorrowerDto = mapper.Map<Borrower, UpdateBorrowerDTO>(borrower);
    updateBorrowerDto.Name = UpdatedBorrowerName;
    Assert.Equal(assertAddress2, updateBorrowerDto.Contact.Address);
    updateBorrowerDto.Contact.Address = address3;

    await ExecuteWithNewDatabaseContext(async databaseContext =>
    {
        _ = await mapper.MapAsync<UpdateBorrowerDTO, Borrower>(updateBorrowerDto, b => b.Include(b => b.Contact), databaseContext);
        _ = await databaseContext.SaveChangesAsync();
        borrower = await databaseContext.Set<Borrower>().Include(b => b.Contact).FirstAsync();
        Assert.Equal(UpdatedBorrowerName, borrower.Name);
        Assert.Equal(assertAddress3, borrower.Contact.Address);
    });
}
```
In the above sample code:
- NewBorrowerDTO contains a navigation property "Contract" of type *NewContactDTO*, which will be automatically mapped to a new instance of *Contact* as navigation property of the newly mapped *Borrower* entity.
- Updated properties of "Contact" property of UpdateBorrowerDTO will be reflected in the "Contact" navigation property of mapped *Borrower* entity.
- In this code example usage of "includer" pamametr of *MapAsync* method is demonstrated, it takes an expression which can be compiled into a function that allows developers to specify the navigation properties to be included by EntityFramework/EntityFrameworkCore when loading the entity. In this example "Contact" navigation property is to be updated, so it should be specified in the "includer" parameter, so DbContact loads this navigation property together with *Borrower* entity in the same database access that later **the library** can directly map "Contact" property of *UpdateBorrowerDTO* to it.
- In the original design, the "includer" parameter is the last parameter of method *MapAsync* with a default value of null, but later during test case write up, it was found that if implemented this way, developers may forget to pass value to this parameter when they should. Hence the parameter is promoted to be the second parameter without a default value, to remind developers to specify navigation properties to include if applicable.
- An interesting question arises here: what if developers still forget to specify "includer" parameter or insist on not assigning value to it when it is actually needed? The answer is: **the library** will try to load the navigation property of entity from database according to identity property of the navigation property during mapping process, if source navigation property is valid but target navigation property is null. If the target navigation property is found in the database, it will be updated; or else mapping of the navigation property will be treated as inserting a new data record of it. This implementation guarantees that mapping process can still go through correctly, if developers don't specify correct value of "includer" parameter when they should.
- The case that navigation property being null for collection type navigation properties is similar, **the library** will try to look into the database for all items in the list which are somehow not included by the includer parameter, then update existing, insert non-existing.
- Note that for every target navigation property being null or has missing items (for collection types), it will cost an extra database access, which affects performance of the program. So the suggestion is not to pass correct value to "includer" paramter, and not to rely on this automatic-database-lookup feature.
#### Excluding Properties for Mapping
If necessary, developers can configure **the library** not to map properties of certain names during mapping. This can be configured at 3 levels
- by default: during mapping, any property has the specific name won't be mapped.
- per class: for specific class, the property with the specific name won't be mapped.
- per mapping: when mapping from one specific class to another, the peroperty with the specific name won't be mapped.
Below are the examples for configuring the 3 cases:
```C#
// by default, which was skipped in the description of MakeDefaultMapperBuilder method
new MapperBuilderFactory()
    .Configure()
        .ExcludedPropertiesByName(excludedProperties)
        .Finish()
    .MakeMapperBuilder();
// per class, for NewContactDTO in this case
MakeDefaultMapperBuilder()
    .Configure<NewContactDTO>()
        .ExcludePropertiesByName(nameof(UpdateContactDTO.Address))
        .Finish()
// per mapping, for mapping from NewContactDTO to Contact in this case
var mapper = MakeDefaultMapperBuilder()
    .Configure<NewContactDTO, Contact>()
        .ExcludePropertiesByName(nameof(Contact.Address))
        .Finish()
```
Note that if *Configure<A, B>()* is called, **the library** will register mapping from class *A* to class *B*, so developers won't need to specify *Register<A, B>* in later configuration. Of course, if they do specify it, it will be simply ignored as redudant registration, no exceptions will be thrown.
### TestCase4_CustomFactoryMethod
**The library** need to create new instances of target entities or collection of target entities during mapping from time to time, and by default, it tries to find the default parameterless constructor of class of the target entity. Considering most entity class should have a such constructor, the approach should work. But what if the target entity doesn't have a default parameterless constructor?
For this case, developers must name a factory method for such target entities, **the library** will use the factory method for the class if any defined is registered. The example is as below:
```C#
var mapper = MakeDefaultMapperBuilder()
    .WithScalarConverter<long, string>(l => l.ToString())
    .WithFactoryMethod<IBookCopyList>(() => new BookCopyList())
    .WithFactoryMethod<IBook>(() => new BookImplementation())
    .Register<NewBookDTO, Book>()
    .Register<Book, IBook>()
    .Build();
```
In this case the target entities are interfaces (*IBooks* and *IBookCopyList*), which don't have constructors at all. It's apparent that the introduction of *WithFactoryMethod* extends supported data scope of **the library** from normal classes with default parameterless constructors to abstract classes and interfaces.
### TestCase5_NavigationPropertyOperation_KeepUnmatched
Recursive mapping section describes the way to update a navigation property from its root entity, what it doesn't describe is what happens if developers replace the nevigation propety value with a totally new one. The answer is: **the library** will replace the old navigation property value with the newly assigned one to behave as expected. As for what happens to the replaced entity, whether it stays in the database or get removed from database, it's out of **the library**'s scope, but up to the database settings. If the nevigation property is set to be cascade on delete, then it will be removed from database upon being replaced, or else it stays in database.
For collection type navigation properties, things are a bit complicated. See the graph below:

![Mapping to Collection Navigation Property Graph](https://github.com/keeper013/Oasis.EFMapper/blob/main/Document/ListNavigationPropertyMapping.png)

The graph shows, when loading value for the collection type navigation property, 4 items are loaded: ABCD; but user inputs CDEF for content of the collection type navigation property. It's easy to understand that during mapping of this property, **the library** would update C and D, insert E and F. The action to take to items A and B hasn't been specified yet.
By default, **the library** assumes that developers originally loaded ABCD from the database, knowlingly removed A and B from the collection, updated C and D, then added E and F. So the correct behavior for this understanding is to remove A and B from the collection. This is a feature to allow developers removing entities with mapping, which could save developers quite some coding effort handling the find and remove logic if without **the library**.
**The library** allows developers to override this feature to keep the loaded but unmatched entities instead by configuration.
It can be configured to an entity, that when mapping to specific collection type navigation properties of this entity, unmatched items will be kept in the collection instead of removed.
```C#
mapperBuilder = mapperBuilder
    .Configure<Book>()
        .KeepUnmatched(nameof(Book.Copies))
        .Finish();
```
Or configured to a mapping scenario when mapping from one specific class to another
```C#
mapperBuilder = mapperBuilder
    .Configure<UpdateBookDTO, Book>()
        .KeepUnmatched(nameof(Book.Copies))
        .Finish();
```
### TestCase6_CustomMapping
By default, **the library** supports:
- mapping between scalar properties with the same name and of the same type (or of the types can be converted by scalar converters).
- mapping between navigation properties with the same name of valid types.

It's possible that more flexible mapping is required, as mapping a property of one name to another of a different name, hence *MapProperty* method is introduced:
```C#
var mapper = MakeDefaultMapperBuilder()
    .Configure<Borrower, BorrowerBriefDTO>()
        .MapProperty(brief => brief.Phone, borrower => borrower.Contact.PhoneNumber)
        .Finish()
```
This configuration specifies that when mapping an instance of *Borrower* to an instance of *BorrowerBriefDTO*, "Phone" property of *BorrowerBriefDTO* should be mapped as configured by the inline method *borrower => borrower.Contact.PhoneNumber*.
### TestCase7_Session
During mapping, there could be cases where multiple entities share some same instances for nevigation entities. Like in this example, many books may share the same tag. During a call of *IMapper.Map* or *IMapper.MapAsync*, **the library** makes sure that each entity is only mapped once, which avoids redundant mapping, and guarantees mapping result is correct.
Examples in this test case uses a *NewBookWithNewTagDTO*, which adds new books together with new tags. Business wise this may not make sense, considering books and tags are not-so-connected entities that are supposed to be managed separately, here we ignore this, and just use it to demonstrate this feature of **the library**.
```C#
var tag = new NewTagDTO { Name = "Tag1" };
var book1 = new NewBookWithNewTagDTO { Name = "Book1" };
book1.Tags.Add(tag);
var book2 = new NewBookWithNewTagDTO { Name = "Book2" };
book2.Tags.Add(tag);
_ = await mapper.MapAsync<NewBookWithNewTagDTO, Book>(book1, null, databaseContext);
_ = await mapper.MapAsync<NewBookWithNewTagDTO, Book>(book2, null, databaseContext);
_ = await databaseContext.SaveChangesAsync();
```
In the sample code above we mean to add 2 new books with the same new tag, mapper.MapAsync is called twice. For the first time, **the library** inserts "Book1" and "Tag1" into the database; for the second time, **the library** tries to insert "Books2" and "Tag1", which triggers a database exception because name of tag is supposed to be unique in the database. The point is, inserting "Tag1" twice isn't the purpose, but since the same instance appears in 2 different calls to *MapAsync*, **the library** doesn't know for the second call, the data presented by the NewTagDTO has been mapped in previous processes that it's not supposed to be inserted again. **The library** only guarantees to map the same instance once per mapping, with *IMapper.Map* or *IMapper.MapAsync* there no way to trace mapped entities between such calls.
To overcome this problem, **the library** provides a session concept to extend the scope of mapping-only-once scenario. *IMapper.CreateMappingSession* creates a map to memory session which can track mapped from entities among as many calls as possible; *IMapper.CreateMappingToDatabaseSession* creates a similar map to database session. I doubt if this use case is needed a lot, but in case it is, the mechanism is provided.
**The library** trackes both hash code of an entity or identity property value of it (for entity to be updated) to judge if an entity has been mapped from.
### TestCase8_InsertUpdateLimit
**The library** provides a safety check mechanism to guarantee correct usage of mappings, which limits insertion/updation when mapping from a DTO class to a database entity class. Like for UpdateBookDTO, if it's only supposed to be used to update an existing book into database, never inserting a new book into database, this can be guaranteed with a configuration.
```C#
var mapper = MakeDefaultMapperBuilder()
    .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
    .Configure<UpdateBookDTO, Book>()
        .SetMapToDatabaseType(MapToDatabaseType.Update)
        .Finish()
    .Build();
```
The focus in the code is *SetMapToDatabaseType* method. If not configured, the default value for all mapping is Upsert, which allows updation and insertion. We can also specify we only want to insert new books with NewBookDTO with the following statement
```C#
.Configure<NewBookDTO, Book>()
    .SetMapToDatabaseType(MapToDatabaseType.Insert)
    .Finish()
```
The thing is NewBookDTO doesn't really have an identity property, so it can't be used to update entities in database anyway, so this configuration may be considered useless.
## Code Structure
## Possible Improvements/Further Ideas
- So far **the library** doesn't support mapping to structures (it's neither designed nor defended against), it may be considered if reasonable requirements comes, at least some defensive code can be added if it's not supposed to be supported.
- **The library** doesn't by default support mapping of collection of scalar types, like **List<int>**, **ICollection<double>**, most likely it will remain like this, because such types are not valid for entities what **the library** focuses on.
- Regarding the "includer" parameter of method *MapAsync*, if developers forget to pass in the value when they should, **the library** will find target navigation properties to be null when they try to do the mapping. Without looking into the database to verify whether data record with the given identity exists, the only thing it can do is trying to insert a new data record into the database according to source navigation properties, which will trigger an database exception if data records with the same identity already exists. Mapping will fail, but data in database remains to be correct. Hence the exception could serve as a reminder to developers to pass in correct value for "includer" parameter of *MapAsync* method. This could be an alternative way of implementation from having **the library** automatically look into the database for missing target navigation properties. Maybe both behaviors can be implented with a switch to decide which is turned on.
## Feedback
There there be any questions or suggestions regarding the library, please send an email to keeper013@gmail.com for inquiry.
When submitting bugs, it's preferred to submit a C# code file with a unit test to easily reproduce the bug.
