namespace SmartLab.TemplateService.Application.Templates.Commands.ExecuteTemplate;

public interface IExecuteTemplateOutputPort
{
    void ResourceNotFound();

    void ConflictDetected();

    void Success(TemplateIdDto templateIdDto);
}
