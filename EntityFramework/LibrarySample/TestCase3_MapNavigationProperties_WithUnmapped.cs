namespace LibrarySample;

using Google.Protobuf;
using Oasis.EntityFramework.Mapper.Sample;
using Oasis.EntityFramework.Mapper;
using NUnit.Framework;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public sealed class TestCase3_MapNavigationProperties_WithUnmapped : TestBase
{
    [Test]
    public async Task Test1_AddAndUpateBorrower()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
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

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, BorrowerAddress, BorrowerAddress, UpdatedBorrowerAddress, UpdatedBorrowerAddress);
    }

    [Test]
    public async Task Test2_AddAndUpateBorrower_WithGlobalUnmappedProperty()
    {
        var mapper = MakeDefaultMapperBuilder(new string[] { nameof(Contact.Address) })
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
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

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, null, null, UpdatedBorrowerAddress, null);
    }

    [Test]
    public async Task Test3_AddAndUpateBorrower_WithTypeUnmappedProperty_NewContactDTO()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<Borrower>()
                .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
                .Finish()
            .Configure<NewBorrowerDTO>()
                .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
                .Finish()
            .Configure<UpdateBorrowerDTO>()
                .SetKeyPropertyNames(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
                .Finish()
            .Configure<NewContactDTO>()
                .ExcludePropertiesByName(nameof(UpdateContactDTO.Address))
                .Finish()
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, null, null, UpdatedBorrowerAddress, UpdatedBorrowerAddress);
    }

    [Test]
    public async Task Test4_AddAndUpateBorrower_WithTypeUnmappedProperty_UpdateContactDTO()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<Borrower>()
                .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
                .Finish()
            .Configure<NewBorrowerDTO>()
                .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
                .Finish()
            .Configure<UpdateBorrowerDTO>()
                .SetKeyPropertyNames(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
                .Finish()
            .Configure<UpdateContactDTO>()
                .ExcludePropertiesByName(nameof(UpdateContactDTO.Address))
                .Finish()
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, BorrowerAddress, null, UpdatedBorrowerAddress, BorrowerAddress);
    }

    [Test]
    public async Task Test5_AddAndUpateBorrower_WithTypeUnmappedProperty_Contact()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<Borrower>()
                .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
                .Finish()
            .Configure<NewBorrowerDTO>()
                .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
                .Finish()
            .Configure<UpdateBorrowerDTO>()
                .SetKeyPropertyNames(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
                .Finish()
            .Configure<Contact>()
                .ExcludePropertiesByName(nameof(Contact.Address))
                .Finish()
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, null, null, UpdatedBorrowerAddress, null);
    }

    [Test]
    public async Task Test6_AddAndUpateBorrower_WithTypeUnmappedProperty_NewDtoToEntity()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<Borrower>()
                .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
                .Finish()
            .Configure<NewBorrowerDTO>()
                .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
                .Finish()
            .Configure<UpdateBorrowerDTO>()
                .SetKeyPropertyNames(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
                .Finish()
            .Configure<NewContactDTO, Contact>()
                .ExcludePropertiesByName(nameof(Contact.Address))
                .Finish()
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, null, null, UpdatedBorrowerAddress, UpdatedBorrowerAddress);
    }

    [Test]
    public async Task Test7_AddAndUpateBorrower_WithTypeUnmappedProperty_EntityToUpdateDto()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<Borrower>()
                .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
                .Finish()
            .Configure<NewBorrowerDTO>()
                .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
                .Finish()
            .Configure<UpdateBorrowerDTO>()
                .SetKeyPropertyNames(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
                .Finish()
            .Configure<Contact, UpdateContactDTO>()
                .ExcludePropertiesByName(nameof(Contact.Address))
                .Finish()
            .Register<Contact, UpdateContactDTO>()
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, BorrowerAddress, null, UpdatedBorrowerAddress, UpdatedBorrowerAddress);
    }

    [Test]
    public async Task Test8_AddAndUpateBorrower_WithTypeUnmappedProperty_UpdateDtoToEntity()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<Borrower>()
                .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
                .Finish()
            .Configure<NewBorrowerDTO>()
                .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
                .Finish()
            .Configure<UpdateBorrowerDTO>()
                .SetKeyPropertyNames(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
                .Finish()
            .Configure<UpdateContactDTO, Contact>()
                .ExcludePropertiesByName(nameof(Contact.Address))
                .Finish()
            .Register<UpdateContactDTO, Contact>()
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, BorrowerAddress, BorrowerAddress, UpdatedBorrowerAddress, BorrowerAddress);
    }

    [Test]
    public async Task Test9_AddBookWithCopies()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .Configure<NewCopyDTO>()
                .SetIdentityPropertyName(nameof(NewCopyDTO.Number))
                .Finish()
            .Configure<Copy>()
                .SetIdentityPropertyName(nameof(Copy.Number))
                .Finish()
            .Register<NewBookDTO, Book>()
            .Register<Book, BookDTO>()
            .Build();

        // create new book
        const string BookName = "Book 1";
        Book book = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var bookDto = new NewBookDTO { Name = BookName };
            for (var i = 0; i < 5; i++)
            {
                bookDto.Copies.Add(new NewCopyDTO { Number = $"Copy{i + 1}" });
            }

            _ = await mapper.MapAsync<NewBookDTO, Book>(bookDto, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().Include(b => b.Copies).FirstAsync();
            Assert.AreEqual(BookName, book.Name);
            for (var i = 0; i < 5; i++)
            {
                Assert.IsTrue(book.Copies.Any(c => string.Equals($"Copy{i + 1}", c.Number)));
            }
        });

        // map from book to dto
        var bookDto = mapper.Map<Book, BookDTO>(book);
        Assert.AreEqual(BookName , bookDto.Name);
        for (var i = 0; i < 5; i++)
        {
            Assert.IsTrue(book.Copies.Any(c => string.Equals($"Copy{i + 1}", c.Number)));
        }
    }

    [Test]
    public async Task Test10_UpdateBookWithExistingTags()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .Register<NewTagDTO, Tag>()
            .Register<Tag, IdReferenceDTO>()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Register<NewBookDTO, Book>()
            .Register<Book, BookDTO>()
            .Build();

        // create new tag
        const string Tag1Name = "English";
        const string Tag2Name = "Fiction";
        List<Tag> tags = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var tag1Dto = new NewTagDTO { Name = Tag1Name };
            var tag2Dto = new NewTagDTO { Name = Tag2Name };
            _ = await mapper.MapAsync<NewTagDTO, Tag>(tag1Dto, null, databaseContext);
            _ = await mapper.MapAsync<NewTagDTO, Tag>(tag2Dto, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            tags = await databaseContext.Set<Tag>().ToListAsync();
            Assert.AreEqual(2, tags.Count);
            Assert.IsTrue(tags.Any(t => string.Equals(Tag1Name, t.Name)));
            Assert.IsTrue(tags.Any(t => string.Equals(Tag2Name, t.Name)));
        });

        // map from tag to dto
        var tag1 = mapper.Map<Tag, IdReferenceDTO>(tags[0]);
        var tag2 = mapper.Map<Tag, IdReferenceDTO>(tags[1]);

        // new book with existing tag
        const string BookName = "Book 1";
        Book book = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var bookDto = new NewBookDTO { Name = BookName };
            bookDto.Tags.Add(tag1);
            bookDto.Tags.Add(tag2);
            _ = await mapper.MapAsync<NewBookDTO, Book>(bookDto, b => b.Include(b => b.Tags), databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().Include(b => b.Tags).FirstAsync();
            Assert.AreEqual(BookName, book.Name);
            Assert.AreEqual(2, book.Tags.Count);
            Assert.IsTrue(book.Tags.Any(t => string.Equals(Tag1Name, t.Name)));
            Assert.IsTrue(book.Tags.Any(t => string.Equals(Tag2Name, t.Name)));
            Assert.AreEqual(2, await databaseContext.Set<Tag>().CountAsync());
        });
    }

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
            Assert.AreEqual(BorrowerName, borrower.Name);
            Assert.AreEqual(assertAddress1, borrower.Contact.Address);
        });

        // update existint book dto
        const string UpdatedBorrowerName = "Updated Borrower 1";
        var updateBorrowerDto = mapper.Map<Borrower, UpdateBorrowerDTO>(borrower);
        updateBorrowerDto.Name = UpdatedBorrowerName;
        Assert.AreEqual(assertAddress2, updateBorrowerDto.Contact.Address);
        updateBorrowerDto.Contact.Address = address3;

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            _ = await mapper.MapAsync<UpdateBorrowerDTO, Borrower>(updateBorrowerDto, b => b.Include(b => b.Contact), databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            borrower = await databaseContext.Set<Borrower>().Include(b => b.Contact).FirstAsync();
            Assert.AreEqual(UpdatedBorrowerName, borrower.Name);
            Assert.AreEqual(assertAddress3, borrower.Contact.Address);
        });
    }
}
