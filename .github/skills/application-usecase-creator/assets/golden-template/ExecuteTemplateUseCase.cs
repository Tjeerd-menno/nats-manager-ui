using Microsoft.Extensions.Logging;
using SmartLab.TemplateService.Domain;
using SmartLab.TemplateService.Domain.Templates;

namespace SmartLab.TemplateService.Application.Templates.Commands.ExecuteTemplate;

public class ExecuteTemplateUseCase(
    ITemplatesRepository templatesRepository,
    IAuditTrailOutputPort auditTrailOutputPort,
    ILogger<ExecuteTemplateUseCase> logger) : IExecuteTemplateUseCase
{
    private readonly ITemplatesRepository templatesRepository = templatesRepository;
    private readonly IAuditTrailOutputPort auditTrailOutputPort = auditTrailOutputPort;
    private readonly ILogger<ExecuteTemplateUseCase> logger = logger;

    public async Task Execute(string userName, ExecuteTemplateInput input, IExecuteTemplateOutputPort outputPort, CancellationToken ct)
    {
        TemplateAggregate? existing = await this.templatesRepository.GetByIdAsync(input.Id, ct);
        if (existing is null)
        {
            outputPort.ResourceNotFound();
            return;
        }

        bool hasConflict = await this.templatesRepository.ExistsByNameAsync(input.Name, ct);
        if (hasConflict)
        {
            outputPort.ConflictDetected();
            return;
        }

        object original = existing.Clone();

        existing.Update(input.Name, input.Description);

        await this.templatesRepository.UpdateAsync(existing, ct);

        string message = $"Updated template with id {existing.Id}";

        await this.auditTrailOutputPort.PublishAuditTrail(
            userName,
            IAuditTrailOutputPort.PublishAction.Update,
            message,
            original,
            existing);

        this.logger.LogInformation("{Message}", message);

        outputPort.Success(new TemplateIdDto(existing.Id));
    }
}
