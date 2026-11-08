using Flatplanner.Api.Chat;

namespace Flatplanner.Tests;

public sealed class OffTopicReplyTests
{
    [Fact]
    public void Compose_AnyIndex_StartsWithLeadAndAddsJoke()
    {
        var first = OffTopicReply.Compose(0);
        var second = OffTopicReply.Compose(1);

        Assert.StartsWith("Nie rozumiem tego pytania", first);
        Assert.NotEqual(first, second);
        Assert.Equal(first.Split(':')[0], second.Split(':')[0]);
    }

    [Fact]
    public void Compose_IndexBeyondPool_Wraps()
    {
        Assert.NotEmpty(OffTopicReply.Compose(int.MaxValue));
    }

    [Fact]
    public void Pick_Always_ReturnsAJokeReply()
    {
        Assert.StartsWith("Nie rozumiem tego pytania", OffTopicReply.Pick());
    }
}
