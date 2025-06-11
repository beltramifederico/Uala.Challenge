using FluentValidation;

namespace Uala.Challenge.Application.Tweets.Commands;

public class CreateTweetCommandValidator : AbstractValidator<CreateTweetCommand>
{
    public CreateTweetCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotNull()
            .WithMessage("Tweet content cannot be null.")
            .NotEmpty()
            .WithMessage("Tweet content cannot be empty.")
            .MaximumLength(280)
            .WithMessage("Tweet content cannot exceed 280 characters.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
