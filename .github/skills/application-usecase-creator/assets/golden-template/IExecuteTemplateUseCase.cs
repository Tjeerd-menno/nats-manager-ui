namespace SmartLab.TemplateService.Application.Templates.Commands.ExecuteTemplate;

public interface IExecuteTemplateUseCase
{
    Task Execute(string userName, ExecuteTemplateInput input, IExecuteTemplateOutputPort outputPort, CancellationToken ct);
}
