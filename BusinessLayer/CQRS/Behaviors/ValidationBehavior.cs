using FluentValidation;
using MediatR;

namespace BusinessLayer.CQRS.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Sequential execution supports validators sharing a scoped EF DbContext.
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
            failures.AddRange((await validator.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken)).Errors);
        if (failures.Count != 0) throw new ValidationException(failures);
        return await next();
    }
}
