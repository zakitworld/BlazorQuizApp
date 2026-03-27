using BlazorQuizApp.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BlazorQuizApp.Tests.Unit
{
    public class CacheServiceTests : IDisposable
    {
        private readonly IMemoryCache _memCache = new MemoryCache(new MemoryCacheOptions());
        private readonly CacheService _sut;

        public CacheServiceTests()
        {
            _sut = new CacheService(_memCache, NullLogger<CacheService>.Instance);
        }

        // ── GetOrSetAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetOrSetAsync_CacheMiss_InvokesFactory()
        {
            var callCount = 0;
            var result = await _sut.GetOrSetAsync<string>("key1", () =>
            {
                callCount++;
                return Task.FromResult("value");
            });

            Assert.Equal("value", result);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetOrSetAsync_CacheHit_DoesNotInvokeFactory()
        {
            var callCount = 0;
            Func<Task<string>> factory = () =>
            {
                callCount++;
                return Task.FromResult("cached");
            };

            await _sut.GetOrSetAsync("key2", factory);
            await _sut.GetOrSetAsync("key2", factory); // second call — should hit cache

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetOrSetAsync_StoresValueInCache()
        {
            await _sut.GetOrSetAsync<string>("key3", () => Task.FromResult("stored"));

            var hit = _memCache.TryGetValue("key3", out string? val);
            Assert.True(hit);
            Assert.Equal("stored", val);
        }

        [Fact]
        public async Task GetOrSetAsync_NullReturnedByFactory_NotCached()
        {
            // Use a wrapper object so we can return null from factory while satisfying where T : class
            var callCount = 0;
            Func<Task<List<string>>> factory = () =>
            {
                callCount++;
                return Task.FromResult<List<string>>(null!);
            };

            var first = await _sut.GetOrSetAsync("key4", factory);
            var second = await _sut.GetOrSetAsync("key4", factory); // factory called again

            Assert.Null(first);
            Assert.Null(second);
            Assert.Equal(2, callCount); // null result is never cached
        }

        [Fact]
        public async Task GetOrSetAsync_DifferentKeys_StoredSeparately()
        {
            await _sut.GetOrSetAsync<string>("keyA", () => Task.FromResult("A"));
            await _sut.GetOrSetAsync<string>("keyB", () => Task.FromResult("B"));

            Assert.Equal("A", await _sut.GetOrSetAsync<string>("keyA", () => Task.FromResult("nope")));
            Assert.Equal("B", await _sut.GetOrSetAsync<string>("keyB", () => Task.FromResult("nope")));
        }

        [Fact]
        public async Task GetOrSetAsync_CustomExpiration_Accepted()
        {
            // Just verify no exception is thrown with a custom expiration
            var result = await _sut.GetOrSetAsync<string>("keyExp", () =>
                Task.FromResult("val"), TimeSpan.FromSeconds(30));

            Assert.Equal("val", result);
        }

        [Fact]
        public async Task GetOrSetAsync_WorksWithComplexObjects()
        {
            var list = new List<string> { "a", "b", "c" };
            var result = await _sut.GetOrSetAsync("keyList", () =>
                Task.FromResult(list));

            Assert.NotNull(result);
            Assert.Equal(3, result!.Count);
        }

        // ── Remove ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task Remove_ExistingKey_SubsequentGetInvokesFactory()
        {
            var callCount = 0;
            Func<Task<string>> factory = () =>
            {
                callCount++;
                return Task.FromResult("v");
            };

            await _sut.GetOrSetAsync("rem1", factory);
            _sut.Remove("rem1");
            await _sut.GetOrSetAsync("rem1", factory);

            Assert.Equal(2, callCount); // factory called twice because key was removed
        }

        [Fact]
        public void Remove_NonExistentKey_NoException()
        {
            var ex = Record.Exception(() => _sut.Remove("does-not-exist"));
            Assert.Null(ex);
        }

        // ── Clear ───────────────────────────────────────────────────────────────

        [Fact]
        public void Clear_DoesNotThrow()
        {
            var ex = Record.Exception(() => _sut.Clear());
            Assert.Null(ex);
        }

        public void Dispose() => _memCache.Dispose();
    }
}
