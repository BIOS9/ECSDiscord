﻿using System.IO;
using System.Text.RegularExpressions;
using FluentValidation;

namespace ECSDiscord.Services.Verification;

public class VerificationOptionsValidation : AbstractValidator<VerificationOptions>
{
    public VerificationOptionsValidation()
    {
        RuleFor(x => x.PublicKeyPath)
            .NotEmpty()
            .Must(File.Exists).When(path => path != null)
            .WithMessage(c => $"Path does not exist \"{c.PublicKeyPath}\"");

        RuleFor(x => x.EmailPattern)
            .NotEmpty()
            .Custom((pattern, context) =>
            {
                try
                {
                    _ = new Regex(pattern);
                }
                catch
                {
                    context.AddFailure("Invalid RegEx pattern");
                }
            });
    }
}