using System.Collections;
using GtaAutoGameplay.Core.Targeting;
using GtaAutoGameplay.Core.Tests.Fakes;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class WindowSelectionTests
{
    private static readonly DateTimeOffset SnapshotTime =
        new(2026, 8, 19, 4, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task Discovery_WithNoWindows_ReturnsSuccessfulEmptySnapshot()
    {
        FakeWindowDiscovery discovery = new([]);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Candidates);
        Assert.IsNull(result.Failure);
        Assert.IsEmpty(discovery.Selections);
    }

    [TestMethod]
    public async Task Discovery_ReturnsOneOrMoreCandidatesWithoutSelectingThem()
    {
        WindowCandidate first = CreateCandidate("first");
        WindowCandidate second = CreateCandidate("second", processId: 202);
        FakeWindowDiscovery discovery = new([first, second]);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEquivalent(
            new[] { first.CandidateId, second.CandidateId },
            result.Candidates.Select(candidate => candidate.CandidateId).ToArray());
        Assert.IsEmpty(discovery.Selections);
    }

    [TestMethod]
    public void DiscoveryResult_RequiresUniqueCandidateIds()
    {
        CandidateId duplicateId = new(Guid.NewGuid());
        WindowCandidate first = CreateCandidate("first", candidateId: duplicateId);
        WindowCandidate second = CreateCandidate("second", candidateId: duplicateId, processId: 202);

        Assert.ThrowsExactly<ArgumentException>(() =>
            WindowDiscoveryResult.Succeeded([first, second]));
    }

    [TestMethod]
    public void CandidateIdAndSelectionId_AreDistinctNonInterchangeableTypes()
    {
        Assert.AreNotEqual(typeof(CandidateId), typeof(SelectionId));
        Assert.IsFalse(typeof(CandidateId).IsAssignableFrom(typeof(SelectionId)));
        Assert.IsFalse(typeof(SelectionId).IsAssignableFrom(typeof(CandidateId)));

        Type candidateParameter = typeof(IWindowDiscovery)
            .GetMethod(nameof(IWindowDiscovery.SelectCandidateAsync))!
            .GetParameters()[0]
            .ParameterType;
        Assert.AreEqual(typeof(CandidateId), candidateParameter);
        Assert.AreEqual(
            typeof(SelectionId),
            typeof(WindowSelection).GetProperty(nameof(WindowSelection.SelectionId))!.PropertyType);
    }

    [TestMethod]
    public async Task Selection_CancelledByUser_ReturnsTypedFailureWithoutSelection()
    {
        WindowCandidate candidate = CreateCandidate("single");
        FakeWindowDiscovery discovery = new([candidate]);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        WindowSelectionResult result = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            SnapshotTime.AddSeconds(1),
            cancellation.Token);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(WindowSelectionFailure.Cancelled, result.Failure);
        Assert.IsNull(result.Selection);
        Assert.IsEmpty(discovery.Selections);
    }

    [TestMethod]
    public async Task Selection_ExpiredCandidate_ReturnsTypedFailure()
    {
        WindowCandidate candidate = CreateCandidate("expired");
        FakeWindowDiscovery discovery = new([candidate]);

        WindowSelectionResult result = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            candidate.ValidUntilUtc,
            CancellationToken.None);

        Assert.AreEqual(WindowSelectionFailure.CandidateExpired, result.Failure);
        Assert.IsNull(result.Selection);
    }

    [TestMethod]
    public async Task Selection_UnavailableCandidate_ReturnsTypedFailure()
    {
        WindowCandidate candidate = CreateCandidate("unavailable");
        FakeWindowDiscovery discovery = new([candidate]);
        discovery.MakeUnavailable(candidate.CandidateId);

        WindowSelectionResult result = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            SnapshotTime.AddSeconds(1),
            CancellationToken.None);

        Assert.AreEqual(WindowSelectionFailure.CandidateUnavailable, result.Failure);
        Assert.IsNull(result.Selection);
    }

    [TestMethod]
    public async Task RepeatedExplicitSelection_CreatesNewSelectionIdentityEachTime()
    {
        WindowCandidate candidate = CreateCandidate("repeat");
        FakeWindowDiscovery discovery = new([candidate]);

        WindowSelectionResult first = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            SnapshotTime.AddSeconds(1),
            CancellationToken.None);
        WindowSelectionResult second = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            SnapshotTime.AddSeconds(2),
            CancellationToken.None);

        Assert.IsTrue(first.IsSuccess);
        Assert.IsTrue(second.IsSuccess);
        Assert.AreNotEqual(first.Selection!.SelectionId, second.Selection!.SelectionId);
        Assert.AreEqual(candidate.CandidateId, first.Selection.CandidateId);
        Assert.AreEqual(candidate.CandidateId, second.Selection.CandidateId);
    }

    [TestMethod]
    public async Task Discovery_WithSingleCandidate_StillRequiresExplicitSelectionCall()
    {
        WindowCandidate candidate = CreateCandidate("only");
        FakeWindowDiscovery discovery = new([candidate]);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.HasCount(1, result.Candidates);
        Assert.IsEmpty(discovery.Selections);

        WindowSelectionResult selection = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            SnapshotTime.AddSeconds(1),
            CancellationToken.None);
        Assert.IsTrue(selection.IsSuccess);
        Assert.HasCount(1, discovery.Selections);
    }

    [TestMethod]
    public async Task Discovery_ObservesCancellationWithoutEnumeratingOrSelecting()
    {
        FakeWindowDiscovery discovery = new([CreateCandidate("candidate")]);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        WindowDiscoveryResult result = await discovery.DiscoverAsync(cancellation.Token);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(WindowDiscoveryFailure.Cancelled, result.Failure);
        Assert.IsEmpty(result.Candidates);
        Assert.IsEmpty(discovery.Selections);
    }

    [TestMethod]
    [DataRow(WindowDiscoveryFailure.Unavailable)]
    [DataRow(WindowDiscoveryFailure.AccessDenied)]
    [DataRow(WindowDiscoveryFailure.EnumerationFailed)]
    [DataRow(WindowDiscoveryFailure.IncompleteMetadata)]
    public async Task DiscoveryFailure_ReturnsTypedFailure(
        WindowDiscoveryFailure failure)
    {
        FakeWindowDiscovery discovery = new([CreateCandidate("candidate")])
        {
            DiscoveryFailure = failure,
        };

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(failure, result.Failure);
        Assert.IsEmpty(result.Candidates);
        Assert.IsEmpty(discovery.Selections);
    }

    [TestMethod]
    public async Task DiscoveryResult_IsDefensiveReadOnlySnapshot()
    {
        List<WindowCandidate> source = [CreateCandidate("first")];
        FakeWindowDiscovery discovery = new(source);
        source.Clear();

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);
        IList nonGenericList = (IList)result.Candidates;

        Assert.HasCount(1, result.Candidates);
        Assert.ThrowsExactly<NotSupportedException>(() =>
            nonGenericList.Add(CreateCandidate("later")));
        Assert.HasCount(1, result.Candidates);
    }

    [TestMethod]
    public async Task Selection_SnapshotsDoNotChangeWhenDiscoveryStateChanges()
    {
        WindowCandidate candidate = CreateCandidate("stable");
        FakeWindowDiscovery discovery = new([candidate]);
        WindowSelectionResult result = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            SnapshotTime.AddSeconds(1),
            CancellationToken.None);

        discovery.MakeUnavailable(candidate.CandidateId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(candidate.CandidateId, result.Selection!.CandidateId);
        Assert.AreSame(candidate.Identity, result.Selection.Identity);
        Assert.IsFalse(result.Selection.IsExpiredAt(SnapshotTime.AddSeconds(2)));
        Assert.IsFalse(
            typeof(WindowSelection).GetProperties().Any(property => property.CanWrite));
        Assert.IsFalse(
            typeof(WindowIdentitySnapshot).GetProperties().Any(property => property.CanWrite));
    }

    [TestMethod]
    public async Task MissingCandidate_UsesTypedFailureInsteadOfExceptionText()
    {
        FakeWindowDiscovery discovery = new([]);

        WindowSelectionResult result = await discovery.SelectCandidateAsync(
            new CandidateId(Guid.NewGuid()),
            SnapshotTime.AddSeconds(1),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(WindowSelectionFailure.CandidateNotFound, result.Failure);
        Assert.IsNull(result.Selection);
    }

    [TestMethod]
    public void TargetingModels_ExposeNoNativeOrWindowsPlatformTypes()
    {
        Type[] targetingTypes = typeof(WindowCandidate).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(WindowCandidate).Namespace)
            .ToArray();

        foreach (Type type in targetingTypes)
        {
            Assert.IsFalse(type.FullName!.Contains("Windows", StringComparison.Ordinal));

            foreach (System.Reflection.PropertyInfo property in type.GetProperties())
            {
                Assert.AreNotEqual(typeof(IntPtr), property.PropertyType);
                Assert.AreNotEqual(
                    true,
                    property.PropertyType.Namespace?.StartsWith("Windows", StringComparison.Ordinal),
                    $"{type.Name}.{property.Name} exposes {property.PropertyType.FullName}.");
            }
        }
    }

    [TestMethod]
    public void IdentitySnapshot_RejectsFullExecutablePathAndInvalidValidity()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            CreateIdentity(executableName: "C:\\Games\\example.exe"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            CreateIdentity(validUntilUtc: SnapshotTime));
    }

    private static WindowCandidate CreateCandidate(
        string suffix,
        CandidateId? candidateId = null,
        int processId = 101) =>
        new(
            candidateId ?? new CandidateId(Guid.NewGuid()),
            $"Test Window {suffix}",
            CreateIdentity(suffix, processId));

    private static WindowIdentitySnapshot CreateIdentity(
        string suffix = "identity",
        int processId = 101,
        string executableName = "controlled-test-window.exe",
        DateTimeOffset? validUntilUtc = null) =>
        new(
            windowInstanceId: $"window-instance-{suffix}",
            processId,
            processInstanceId: $"process-instance-{suffix}",
            processStartedAtUtc: SnapshotTime.AddMinutes(-5),
            windowClassName: "ControlledTestWindow",
            executableName,
            executableIdentity: $"executable-identity-{suffix}",
            capturedAtUtc: SnapshotTime,
            validUntilUtc: validUntilUtc ?? SnapshotTime.AddSeconds(10));
}
