using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace PDS.ParameterTransformers;

public class ApiPrefixConvention : IApplicationModelConvention
{
    private const string Prefix = "api/v1";

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel is not null
                    ? new AttributeRouteModel
                    {
                        Template = AttributeRouteModel.CombineTemplates(Prefix, selector.AttributeRouteModel.Template)
                    }
                    : new AttributeRouteModel { Template = Prefix };
            }
        }
    }
}
