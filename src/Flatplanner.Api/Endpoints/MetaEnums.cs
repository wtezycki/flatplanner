namespace Flatplanner.Api.Endpoints;

public sealed record MetaEnums(
    IReadOnlyList<EnumOption> Market,
    IReadOnlyList<EnumOption> AdvertiserType,
    IReadOnlyList<EnumOption> FinishCondition,
    IReadOnlyList<EnumOption> Ownership,
    IReadOnlyList<EnumOption> FactStatus,
    IReadOnlyList<EnumOption> Sort,
    IReadOnlyList<EnumOption> Provenance);
