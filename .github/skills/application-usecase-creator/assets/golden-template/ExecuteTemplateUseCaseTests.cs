using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NSubstitute;
using SmartLab.TemplateService.Application.Templates.Commands.ExecuteTemplate;
using SmartLab.TemplateService.Domain;
using SmartLab.TemplateService.Domain.Templates;

namespace SmartLab.TemplateService.Application.Tests.Templates.Commands.ExecuteTemplate;

public class ExecuteTemplateUseCaseTests
{
    private readonly IFixture fixture;

    public ExecuteTemplateUseCaseTests()
    {
        this.fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
    }

    [Fact]
    public async Task Execute_succeeded_when_resource_exists_and_name_has_no_conflict()
    {
        IExecuteTemplateOutputPort outputPort = this.fixture.Freeze<IExecuteTemplateOutputPort>();
        ITemplatesRepository repository = this.fixture.Freeze<ITemplatesRepository>();
        IAuditTrailOutputPort auditTrailOutputPort = this.fixture.Freeze<IAuditTrailOutputPort>();
        TemplateAggregate aggregate = this.fixture.Create<TemplateAggregate>();
        ExecuteTemplateInput input = this.fixture.Create<ExecuteTemplateInput>();

        repository.GetByIdAsync(input.Id, Arg.Any<CancellationToken>()).Returns(aggregate);
        repository.ExistsByNameAsync(input.Name, Arg.Any<CancellationToken>()).Returns(false);

        ExecuteTemplateUseCase sut = this.fixture.Create<ExecuteTemplateUseCase>();

        await sut.Execute("user-a", input, outputPort, CancellationToken.None);

        await repository.Received(1).UpdateAsync(aggregate, Arg.Any<CancellationToken>());
        await auditTrailOutputPort.Received(1).PublishAuditTrail(
            "user-a",
            IAuditTrailOutputPort.PublishAction.Update,
            Arg.Any<string>(),
            Arg.Any<object>(),
            aggregate);
        outputPort.Received(1).Success(Arg.Any<TemplateIdDto>());
    }
}
