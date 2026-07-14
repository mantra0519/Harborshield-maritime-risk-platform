using FluentValidation;

namespace HarborShield.Application.Webhooks.Commands.RegisterWebhookEndpoint;

public class RegisterWebhookEndpointCommandValidator : AbstractValidator<RegisterWebhookEndpointCommand>
{
    public RegisterWebhookEndpointCommandValidator()
    {
        RuleFor(c => c.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Url).NotEmpty().Must(BeAnAbsoluteUrl).WithMessage("'Url' must be a valid absolute URL.");
        RuleFor(c => c.Secret).NotEmpty().MinimumLength(16).WithMessage("'Secret' must be at least 16 characters.");
        RuleFor(c => c.SubscribedEventTypes)
            .NotEmpty()
            .Must(types => types.All(t => WebhookEventTypes.RiskCaseCreated == t || WebhookEventTypes.RiskCaseResolved == t))
            .WithMessage($"'{nameof(RegisterWebhookEndpointCommand.SubscribedEventTypes)}' may only contain '{WebhookEventTypes.RiskCaseCreated}' or '{WebhookEventTypes.RiskCaseResolved}'.");
    }

    private static bool BeAnAbsoluteUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}
