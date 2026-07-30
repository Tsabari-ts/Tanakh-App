using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Model;
using Tanakh.Infrastructure.Options;
using Tanakh.Infrastructure.Services;
using Xunit;

namespace Tanakh.Tests
{
    public class NextChapterResolverTests
    {
        private sealed class FakeStructureService : ITanakhStructureService
        {
            private readonly List<BaseStructure> books;

            public FakeStructureService(List<BaseStructure> books) => this.books = books;

            public Task<List<BaseStructure>> GetAllAsync(CancellationToken cancellationToken) =>
                Task.FromResult(books);

            public Task<List<BaseStructure>> GetBySectionAsync(string section, CancellationToken cancellationToken) =>
                Task.FromResult(books.Where(b => b.section == section).ToList());

            public Task<List<BaseStructure>> GetByTitleAsync(string title, CancellationToken cancellationToken) =>
                Task.FromResult(books.Where(b => b.title == title).ToList());
        }

        private sealed class FakeReadingProgressService : IReadingProgressService
        {
            private readonly IReadOnlyList<ReadingProgress> progress;

            public FakeReadingProgressService(IReadOnlyList<ReadingProgress> progress) => this.progress = progress;

            public Task<IReadOnlyList<ReadingProgress>> GetProgressAsync(Guid subscriberId, CancellationToken cancellationToken = default) =>
                Task.FromResult(progress);

            public Task UpsertProgressAsync(Guid subscriberId, ReadingSection section, string book, int chapter, int? verse, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();
        }

        private static List<BaseStructure> ThreeBookFixture() =>
        [
            new() { section = "Torah", title = "Genesis", book = "Genesis", heTitle = "בראשית", heBook = "בראשית", length = 2, chapters = [10, 10] },
            new() { section = "Torah", title = "Exodus", book = "Exodus", heTitle = "שמות", heBook = "שמות", length = 1, chapters = [10] },
            new() { section = "Prophets", title = "Joshua", book = "Joshua", heTitle = "יהושע", heBook = "יהושע", length = 2, chapters = [10, 12] }
        ];

        private static RemindersOptions DefaultOptions() => new() { DefaultStartBook = "Genesis", DefaultStartChapter = 1 };

        private static NextChapterResolver CreateResolver(List<BaseStructure> books, IReadOnlyList<ReadingProgress> progress) =>
            new(new FakeReadingProgressService(progress), new FakeStructureService(books), Options.Create(DefaultOptions()));

        [Fact]
        public async Task ResolveAsync_NoProgress_ReturnsDefaultStart()
        {
            NextChapterResolver resolver = CreateResolver(ThreeBookFixture(), []);

            NextChapterResult result = await resolver.ResolveAsync(Guid.CreateVersion7());

            Assert.Equal("Genesis", result.Book);
            Assert.Equal(1, result.Chapter);
            Assert.False(result.CompletedCycle);
        }

        [Fact]
        public async Task ResolveAsync_MidBook_ReturnsNextChapterSameBook()
        {
            ReadingProgress progress = new() { Id = Guid.CreateVersion7(), SubscriberId = Guid.CreateVersion7(), Section = ReadingSection.Torah, Book = "Genesis", Chapter = 1 };
            NextChapterResolver resolver = CreateResolver(ThreeBookFixture(), [progress]);

            NextChapterResult result = await resolver.ResolveAsync(progress.SubscriberId);

            Assert.Equal("Genesis", result.Book);
            Assert.Equal(2, result.Chapter);
            Assert.False(result.CompletedCycle);
        }

        [Fact]
        public async Task ResolveAsync_EndOfBook_AdvancesToNextBookInCanonicalOrder()
        {
            ReadingProgress progress = new() { Id = Guid.CreateVersion7(), SubscriberId = Guid.CreateVersion7(), Section = ReadingSection.Torah, Book = "Genesis", Chapter = 2 };
            NextChapterResolver resolver = CreateResolver(ThreeBookFixture(), [progress]);

            NextChapterResult result = await resolver.ResolveAsync(progress.SubscriberId);

            Assert.Equal("Exodus", result.Book);
            Assert.Equal(1, result.Chapter);
            Assert.False(result.CompletedCycle);
        }

        [Fact]
        public async Task ResolveAsync_EndOfTanakh_WrapsToDefaultStartWithCompletedCycle()
        {
            ReadingProgress progress = new() { Id = Guid.CreateVersion7(), SubscriberId = Guid.CreateVersion7(), Section = ReadingSection.Neviim, Book = "Joshua", Chapter = 2 };
            NextChapterResolver resolver = CreateResolver(ThreeBookFixture(), [progress]);

            NextChapterResult result = await resolver.ResolveAsync(progress.SubscriberId);

            Assert.Equal("Genesis", result.Book);
            Assert.Equal(1, result.Chapter);
            Assert.True(result.CompletedCycle);
        }

        [Fact]
        public async Task ResolveAsync_UsesTheMostRecentlyUpdatedProgressAcrossSections()
        {
            Guid subscriberId = Guid.CreateVersion7();
            ReadingProgress older = new() { Id = Guid.CreateVersion7(), SubscriberId = subscriberId, Section = ReadingSection.Torah, Book = "Genesis", Chapter = 1, UpdatedAt = DateTimeOffset.UtcNow.AddDays(-5) };
            ReadingProgress newer = new() { Id = Guid.CreateVersion7(), SubscriberId = subscriberId, Section = ReadingSection.Neviim, Book = "Joshua", Chapter = 1, UpdatedAt = DateTimeOffset.UtcNow };
            NextChapterResolver resolver = CreateResolver(ThreeBookFixture(), [older, newer]);

            NextChapterResult result = await resolver.ResolveAsync(subscriberId);

            Assert.Equal("Joshua", result.Book);
            Assert.Equal(2, result.Chapter);
        }
    }
}
