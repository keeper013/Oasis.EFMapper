namespace EfMapperDemo;

using Google.Protobuf;
using NUnit.Framework;
using Oasis.EntityFramework.Mapper;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SQLite;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;

public sealed class Test
{
    private DbConnection? _connection;

    [Test]
    public async Task DemoTest()
    {
        await InitializeData();
        var message = await RetrieveProjectsFromServer();
        var updateMessage = ProcessDataAtClient(message);
        await UpdateDataAtServer(updateMessage);
    }

    private async Task InitializeData()
    {
        var project1 = new ProjectDTO { Name = "Project 1", Description = "Project 1 description." };
        var project2 = new ProjectDTO { Name = "Project 2", Description = "Project 2 description." };
        project1.Employees.Add(new EmployeeDTO { Name = "Employee 1", Description = "Employee 1 description." });
        project1.Employees.Add(new EmployeeDTO { Name = "Employee 2", Description = "Employee 2 description." });
        project2.Employees.Add(new EmployeeDTO { Name = "Employee 3", Description = "Employee 3 description." });
        project2.Employees.Add(new EmployeeDTO { Name = "Employee 4", Description = "Employee 4 description." });
        var employee5 = new EmployeeDTO { Name = "Employee 5", Description = "Employee 5 description." };

        var mapper = new MapperBuilderFactory()
            .Configure()
                .SetIdentityPropertyName("Name")
                .Finish()
            .MakeMapperBuilder()
            .Register<ProjectDTO, Project>()
            .Build();

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            _ = await mapper.MapAsync<ProjectDTO, Project>(project1, null, databaseContext);
            _ = await mapper.MapAsync<ProjectDTO, Project>(project2, null, databaseContext);
            _ = await mapper.MapAsync<EmployeeDTO, Employee>(employee5, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
        });

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var projects = await databaseContext.Set<Project>().Include(p => p.Employees).ToListAsync();
            Assert.AreEqual(2, projects.Count);
            var p1 = projects.FirstOrDefault(p => string.Equals("Project 1", p.Name));
            Assert.NotNull(p1);
            Assert.AreEqual("Project 1 description.", p1.Description);
            Assert.AreEqual(2, p1.Employees.Count);
            Assert.AreEqual(1, p1.Employees.Where(e => string.Equals("Employee 1", e.Name) && string.Equals("Employee 1 description.", e.Description)).Count());
            Assert.AreEqual(1, p1.Employees.Where(e => string.Equals("Employee 2", e.Name) && string.Equals("Employee 2 description.", e.Description)).Count());

            var p2 = projects.FirstOrDefault(p => string.Equals("Project 2", p.Name));
            Assert.NotNull(p2);
            Assert.AreEqual("Project 2 description.", p2.Description);
            Assert.AreEqual(2, p2.Employees.Count);
            Assert.AreEqual(1, p2.Employees.Where(e => string.Equals("Employee 3", e.Name) && string.Equals("Employee 3 description.", e.Description)).Count());
            Assert.AreEqual(1, p2.Employees.Where(e => string.Equals("Employee 4", e.Name) && string.Equals("Employee 4 description.", e.Description)).Count());
        });
    }

    private async Task<byte[]> RetrieveProjectsFromServer()
    {
        var mapper = new MapperBuilderFactory()
            .Configure()
                .SetIdentityPropertyName("Name")
                .Finish()
            .MakeMapperBuilder()
            .Register<Project, ProjectDTO>()
            .Build();

        var allData = new AllDataDTO();
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            foreach (var project in await databaseContext.Set<Project>().ToListAsync())
            {
                allData.Projects.Add(mapper.Map<Project, ProjectDTO>(project));
            }

            foreach (var employee in await databaseContext.Set<Employee>().ToListAsync())
            {
                allData.Employees.Add(mapper.Map<Employee, EmployeeDTO>(employee));
            }
        });

        Assert.AreEqual(2, allData.Projects.Count);
        Assert.AreEqual(1, allData.Projects.Where(p => string.Equals("Project 1", p.Name) && string.Equals("Project 1 description.", p.Description)).Count());
        Assert.AreEqual(1, allData.Projects.Where(p => string.Equals("Project 2", p.Name) && string.Equals("Project 2 description.", p.Description)).Count());

        Assert.AreEqual(5, allData.Employees.Count);
        Assert.AreEqual(1, allData.Employees.Where(e => string.Equals("Employee 1", e.Name) && string.Equals("Employee 1 description.", e.Description)).Count());
        Assert.AreEqual(1, allData.Employees.Where(e => string.Equals("Employee 2", e.Name) && string.Equals("Employee 2 description.", e.Description)).Count());
        Assert.AreEqual(1, allData.Employees.Where(e => string.Equals("Employee 3", e.Name) && string.Equals("Employee 3 description.", e.Description)).Count());
        Assert.AreEqual(1, allData.Employees.Where(e => string.Equals("Employee 4", e.Name) && string.Equals("Employee 4 description.", e.Description)).Count());
        Assert.AreEqual(1, allData.Employees.Where(e => string.Equals("Employee 5", e.Name) && string.Equals("Employee 5 description.", e.Description)).Count());

        return allData.ToByteArray();
    }

    private byte[] ProcessDataAtClient(byte[] content)
    {
        var allData = AllDataDTO.Parser.ParseFrom(content);
        var p1 = allData.Projects.First(p => string.Equals("Project 1", p.Name));
        var p2 = allData.Projects.First(p => string.Equals("Project 2", p.Name));
        var e1 = allData.Employees.First(e => string.Equals("Employee 1", e.Name));
        var e2 = allData.Employees.First(e => string.Equals("Employee 2", e.Name));
        var e3 = allData.Employees.First(e => string.Equals("Employee 3", e.Name));
        var e4 = allData.Employees.First(e => string.Equals("Employee 4", e.Name));
        var e5 = allData.Employees.First(e => string.Equals("Employee 5", e.Name));
        p1.Description = "Almost done";
        p1.Employees.Add(e1);
        p2.Employees.Add(e3);
        var newProject = new ProjectDTO { Name = "New Project", Description = "Project number 3" };
        e2.Description = "Second Employee";
        newProject.Employees.Add(e2);
        newProject.Employees.Add(e5);
        newProject.Employees.Add(new EmployeeDTO { Name = "New Employee", Description = "Employee Number 6" });
        allData.Projects.Add(newProject);

        return allData.ToByteArray();
    }

    private async Task UpdateDataAtServer(byte[] content)
    {
        var mapper = new MapperBuilderFactory()
            .Configure()
                .SetIdentityPropertyName("Name")
                .Finish()
            .MakeMapperBuilder()
            .Register<ProjectDTO, Project>()
            .Build();

        var allData = AllDataDTO.Parser.ParseFrom(content);

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            foreach (var project in allData.Projects)
            {
                _ = await mapper.MapAsync<ProjectDTO, Project>(project, p => p.Include(p => p.Employees), databaseContext);
            }

            _ = await databaseContext.SaveChangesAsync();
        });

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var projects = await databaseContext.Set<Project>().Include(p => p.Employees).ToListAsync();
            Assert.AreEqual(3, projects.Count);
            var p1 = projects.FirstOrDefault(p => string.Equals("Project 1", p.Name));
            Assert.NotNull(p1);
            Assert.AreEqual("Almost done", p1.Description);
            Assert.AreEqual(1, p1.Employees.Count);
            Assert.AreEqual(1, p1.Employees.Where(e => string.Equals("Employee 1", e.Name) && string.Equals("Employee 1 description.", e.Description)).Count());

            var p2 = projects.FirstOrDefault(p => string.Equals("Project 2", p.Name));
            Assert.NotNull(p2);
            Assert.AreEqual("Project 2 description.", p2.Description);
            Assert.AreEqual(1, p2.Employees.Count);
            Assert.AreEqual(1, p2.Employees.Where(e => string.Equals("Employee 3", e.Name) && string.Equals("Employee 3 description.", e.Description)).Count());

            var p3 = projects.FirstOrDefault(p => string.Equals("New Project", p.Name));
            Assert.NotNull(p3);
            Assert.AreEqual("Project number 3", p3.Description);
            Assert.AreEqual(3, p3.Employees.Count);
            Assert.AreEqual(1, p3.Employees.Where(e => string.Equals("Employee 2", e.Name) && string.Equals("Second Employee", e.Description)).Count());
            Assert.AreEqual(1, p3.Employees.Where(e => string.Equals("Employee 5", e.Name) && string.Equals("Employee 5 description.", e.Description)).Count());
            Assert.AreEqual(1, p3.Employees.Where(e => string.Equals("New Employee", e.Name) && string.Equals("Employee Number 6", e.Description)).Count());

            var e4 = await databaseContext.Set<Employee>().Where(e => string.Equals("Employee 4", e.Name)).FirstAsync();
            Assert.AreEqual("Employee 4 description.", e4.Description);
            Assert.True(string.IsNullOrEmpty(e4.ProjectName));
        });
    }

    [SetUp]
    public void Setup()
    {
        _connection = new SQLiteConnection("Data Source=:memory:");
        _connection.Open();
        var sql = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}/script.sql");
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    private async Task ExecuteWithNewDatabaseContext(Func<DbContext, Task> action)
    {
        using var databaseContext = new DatabaseContext(_connection!);
        await action(databaseContext);
    }
}
