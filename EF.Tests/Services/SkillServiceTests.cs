using EmployeeManagement.DTOs.Common;
using EmployeeManagement.DTOs.EmployeeSkills;
using EmployeeManagement.Exceptions;
using EmployeeManagement.Models;
using EmployeeManagement.Repositories.Interfaces;
using EmployeeManagement.Services;
using NSubstitute;

namespace EF.Tests.Services;

public class SkillServiceTests
{
    private readonly ISkillRepository _repo = Substitute.For<ISkillRepository>();
    private readonly SkillService _sut;
    private readonly CancellationToken _ct = new CancellationTokenSource().Token;

    public SkillServiceTests() => _sut = new SkillService(_repo);

    [Fact]
    public async Task Create_throws_Conflict_when_the_name_exists()
    {
        _repo.ExistsByNameAsync("C#", _ct).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(new CreateSkillDto { Name = "C#" }, _ct));
        await _repo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Create_trims_the_name_for_both_the_check_and_the_save()
    {
        _repo.ExistsByNameAsync("Go", _ct).Returns(false);

        await _sut.CreateAsync(new CreateSkillDto { Name = "  Go  " }, _ct);

        await _repo.Received(1).ExistsByNameAsync("Go", _ct);
        await _repo.Received(1).AddAsync(Arg.Is<Skill>(s => s.Name == "Go"), _ct);
    }

    [Fact]
    public async Task Create_returns_the_new_id()
    {
        _repo.When(r => r.AddAsync(Arg.Any<Skill>(), _ct)).Do(c => c.Arg<Skill>().Id = 42);

        Assert.Equal(42, await _sut.CreateAsync(new CreateSkillDto { Name = "Rust" }, _ct));
    }

    [Fact]
    public async Task GetPaged_builds_the_envelope()
    {
        _repo.GetPagedAsync(3, 10, _ct).Returns((new List<Skill> { TestData.Skill() }, 21));

        var result = await _sut.GetPagedAsync(new PagedQuery { Page = 3, PageSize = 10 }, _ct);

        Assert.Equal("C#", result.Items.Single().Name);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetById_returns_null_when_missing()
    {
        _repo.GetByIdAsync(9, _ct).Returns((Skill?)null);

        Assert.Null(await _sut.GetByIdAsync(9, _ct));
    }

    [Fact]
    public async Task Delete_returns_false_when_missing()
    {
        _repo.GetByIdAsync(9, _ct).Returns((Skill?)null);

        Assert.False(await _sut.DeleteAsync(9, _ct));
    }

    [Fact]
    public async Task Delete_removes_the_skill()
    {
        var skill = TestData.Skill();
        _repo.GetByIdAsync(1, _ct).Returns(skill);

        Assert.True(await _sut.DeleteAsync(1, _ct));
        await _repo.Received(1).DeleteAsync(skill, _ct);
    }
}
