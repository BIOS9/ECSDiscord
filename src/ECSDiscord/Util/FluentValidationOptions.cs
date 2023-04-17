using System;
using System.Collections.Generic;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;

namespace ECSDiscord.Util;

public class FluentValidationOptions<TOptions> 
    : IValidateOptions<TOptions> where TOptions : class
{
    private readonly IValidator<TOptions> _validator;
    
    public FluentValidationOptions(IValidator<TOptions> validator)
    {
        _validator = validator;
    }

    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        // Ensure options are provided to validate against
        ArgumentNullException.ThrowIfNull(options);

        // Run the validation
        ValidationResult results = _validator.Validate(options);
        if (results.IsValid)
        {
            // All good!
            return ValidateOptionsResult.Success;
        }

        // Validation failed, so build the error message
        string typeName = options.GetType().Name;
        var errors = new List<string>();
        foreach (var result in results.Errors)
        {
            errors.Add($"Fluent validation failed for '{typeName}.{result.PropertyName}' with the error: '{result.ErrorMessage}'.");
        }

        return ValidateOptionsResult.Fail(errors);
    }
}
