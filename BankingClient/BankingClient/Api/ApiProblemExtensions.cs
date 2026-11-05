using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BankingClient.Api;

/// <summary>
/// A static class that provides an extension method for adding API problem details to a model state dictionary. This allows for the integration of API validation errors into the ASP.NET Core model binding and validation framework, enabling the display of error messages in views or API responses.
/// </summary>
public static class ApiProblemExtensions
{
    /// <summary>
    /// Adds the details of an <see cref="ApiProblem"/> to the specified <see cref="ModelStateDictionary"/>, allowing for the integration of API validation errors into the ASP.NET Core model binding and validation framework. If the problem contains no specific field errors, a general error message is added; otherwise, each field-specific error is added to the model state.
    /// </summary>
    /// <param name="modelState"></param>
    /// <param name="problem"></param>
    public static void AddApiProblem(this ModelStateDictionary modelState, ApiProblem problem)
    {
        if (problem.Errors.Count == 0)
        {
            modelState.AddModelError(string.Empty, problem.Detail ?? problem.Title);
            return;
        }

        foreach (var (field, messages) in problem.Errors)
        {
            foreach (var message in messages)
            {
                modelState.AddModelError(field, message);
            }
        }
    }
}
