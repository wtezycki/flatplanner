using Flatplanner.Api.Chat;
using Flatplanner.Core.Search;

namespace Flatplanner.Tests;

public sealed class AskConversationTests
{
    private const int MaxLength = 300;

    [Fact]
    public void Validate_FourPreviousMessages_IsValid()
    {
        var history = new[] { "a", "b", "c", "d" };

        Assert.Null(AskValidation.Validate("e", history, MaxLength));
    }

    [Fact]
    public void Validate_FivePreviousMessages_RejectsHistory()
    {
        var history = Enumerable.Repeat("a", SearchDefinitions.MaxHistory + 1).ToArray();

        var errors = AskValidation.Validate("b", history, MaxLength);

        Assert.NotNull(errors);
        Assert.Contains("history", errors.Keys);
    }

    [Fact]
    public void Validate_EmptyHistoryEntry_RejectsHistory()
    {
        var errors = AskValidation.Validate("a", [""], MaxLength);

        Assert.NotNull(errors);
        Assert.Contains("history", errors.Keys);
    }

    [Fact]
    public void Validate_TooLongHistoryEntryOrMessage_RejectsBoth()
    {
        var tooLong = new string('x', MaxLength + 1);

        var errors = AskValidation.Validate(tooLong, [tooLong], MaxLength);

        Assert.NotNull(errors);
        Assert.Contains("message", errors.Keys);
        Assert.Contains("history", errors.Keys);
    }

    [Fact]
    public void InterpretationKey_SameMessageWithDifferentHistory_DiffersAndIgnoresCase()
    {
        var alone = AskService.InterpretationKey([], "a w Gdyni?");
        var refined = AskService.InterpretationKey(["2 pokoje w Gdańsku"], "a w Gdyni?");
        var refinedUpper = AskService.InterpretationKey(["2 POKOJE W GDAŃSKU"], "A w Gdyni?");

        Assert.NotEqual(alone, refined);
        Assert.Equal(refined, refinedUpper);
    }

    [Fact]
    public void ConversationText_NoHistory_IsTheMessage()
    {
        Assert.Equal("2 pokoje", IntentInterpreter.ConversationText([], "2 pokoje"));
    }

    [Fact]
    public void ConversationText_WithHistory_ListsPreviousMessagesOldestFirst()
    {
        var text = IntentInterpreter.ConversationText(["2 pokoje w Gdańsku", "do 700 tys."], "a w Gdyni?");

        Assert.Equal(
            "Previous messages, oldest first:\n1. 2 pokoje w Gdańsku\n2. do 700 tys.\n\nNew message: a w Gdyni?",
            text);
    }
}
