using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BankingClient.Api;

public static class ApiProblemExtensions
{
    /// <summary>
    /// Shows an API problem on the form that caused it: field errors beside their fields
    /// (the API names fields the way view models do), otherwise one message for the whole form.
    /// </summary>
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
