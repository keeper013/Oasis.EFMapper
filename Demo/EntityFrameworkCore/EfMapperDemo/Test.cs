namespace EfMapperDemo;

using Google.Protobuf;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.EfMapperDemo;
using Oasis.EntityFrameworkCore.Mapper;
using System;
using System.Data.Common;
using Xunit;

public sealed class Test : IDisposable
{
    private readonly DbContextOptions _options;
    private readonly DbConnection _connection;

    public Test()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(_connection)
            .Options;
    }

    [Fact]
    public async Task Test1()
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
            Assert.Equal(2, projects.Count);
            var p1 = projects.FirstOrDefault(p => string.Equals("Project 1", p.Name));
            Assert.NotNull(p1);
            Assert.Equal("Project 1 description.", p1.Description);
            Assert.Equal(2, p1.Employees.Count);
            Assert.Single(p1.Employees.Where(e => string.Equals("Employee 1", e.Name) && string.Equals("Employee 1 description.", e.Description)));
            Assert.Single(p1.Employees.Where(e => string.Equals("Employee 2", e.Name) && string.Equals("Employee 2 description.", e.Description)));
            
            var p2 = projects.FirstOrDefault(p => string.Equals("Project 2", p.Name));
            Assert.NotNull(p2);
            Assert.Equal("Project 2 description.", p2.Description);
            Assert.Equal(2, p2.Employees.Count);
            Assert.Single(p2.Employees.Where(e => string.Equals("Employee 3", e.Name) && string.Equals("Employee 3 description.", e.Description)));
            Assert.Single(p2.Employees.Where(e => string.Equals("Employee 4", e.Name) && string.Equals("Employee 4 description.", e.Description)));
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

        Assert.Equal(2, allData.Projects.Count);
        Assert.Single(allData.Projects.Where(p => string.Equals("Project 1", p.Name) && string.Equals("Project 1 description.", p.Description)));
        Assert.Single(allData.Projects.Where(p => string.Equals("Project 2", p.Name) && string.Equals("Project 2 description.", p.Description)));
        
        Assert.Equal(5, allData.Employees.Count);
        Assert.Single(allData.Employees.Where(e => string.Equals("Employee 1", e.Name) && string.Equals("Employee 1 description.", e.Description)));
        Assert.Single(allData.Employees.Where(e => string.Equals("Employee 2", e.Name) && string.Equals("Employee 2 description.", e.Description)));
        Assert.Single(allData.Employees.Where(e => string.Equals("Employee 3", e.Name) && string.Equals("Employee 3 description.", e.Description)));
        Assert.Single(allData.Employees.Where(e => string.Equals("Employee 4", e.Name) && string.Equals("Employee 4 description.", e.Description)));
        Assert.Single(allData.Employees.Where(e => string.Equals("Employee 5", e.Name) && string.Equals("Employee 5 description.", e.Description)));

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
            Assert.Equal(3, projects.Count);
            var p1 = projects.FirstOrDefault(p => string.Equals("Project 1", p.Name));
            Assert.NotNull(p1);
            Assert.Equal("Almost done", p1.Description);
            Assert.Equal(1, p1.Employees.Count);
            Assert.Single(p1.Employees.Where(e => string.Equals("Employee 1", e.Name) && string.Equals("Employee 1 description.", e.Description)));
            
            var p2 = projects.FirstOrDefault(p => string.Equals("Project 2", p.Name));
            Assert.NotNull(p2);
            Assert.Equal("Project 2 description.", p2.Description);
            Assert.Equal(1, p2.Employees.Count);
            Assert.Single(p2.Employees.Where(e => string.Equals("Employee 3", e.Name) && string.Equals("Employee 3 description.", e.Description)));
            
            var p3 = projects.FirstOrDefault(p => string.Equals("New Project", p.Name));
            Assert.NotNull(p3);
            Assert.Equal("Project number 3", p3.Description);
            Assert.Equal(3, p3.Employees.Count);
            Assert.Single(p3.Employees.Where(e => string.Equals("Employee 2", e.Name) && string.Equals("Second Employee", e.Description)));
            Assert.Single(p3.Employees.Where(e => string.Equals("Employee 5", e.Name) && string.Equals("Employee 5 description.", e.Description)));
            Assert.Single(p3.Employees.Where(e => string.Equals("New Employee", e.Name) && string.Equals("Employee Number 6", e.Description)));

            var e4 = await databaseContext.Set<Employee>().Where(e => string.Equals("Employee 4", e.Name)).FirstAsync();
            Assert.Equal("Employee 4 description.", e4.Description);
            Assert.True(string.IsNullOrEmpty(e4.ProjectName));
        });
    }

    public void Dispose() => _connection.Dispose();

    private async Task ExecuteWithNewDatabaseContext(Func<DbContext, Task> action)
    {
        using var databaseContext = new DatabaseContext(_options);
        databaseContext.Database.EnsureCreated();
        await action(databaseContext);
    }
}