﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NSubstitute;
using Octokit.Internal;
using Xunit;
using System.Globalization;

namespace Octokit.Tests.Clients
{
    public class MiscellaneousClientTests
    {
        public class TheRenderRawMarkdownMethod
        {
            [Fact]
            public async Task RequestsTheRawMarkdownEndpoint()
            {
                var markdown = "**Test**";
                var response = "<strong>Test</strong>";
                var apiConnection = Substitute.For<IApiConnection>();
                apiConnection.Post<string>(
                        Arg.Is<Uri>(u => u.ToString() == "markdown/raw"),
                        markdown,
                        "text/html",
                        "text/plain")
                    .Returns(Task.FromResult(response));
                var client = new MiscellaneousClient(apiConnection);

                var html = await client.RenderRawMarkdown(markdown);

                Assert.Equal("<strong>Test</strong>", html);
                apiConnection.Received()
                    .Post<string>(Arg.Is<Uri>(u => u.ToString() == "markdown/raw"),
                    markdown,
                    "text/html",
                    "text/plain");
            }
        }
        public class TheRenderArbitraryMarkdownMethod
        {
            [Fact]
            public async Task RequestsTheMarkdownEndpoint()
            {
                var response = "<strong>Test</strong>";

                var payload = new NewArbitraryMarkdown("testMarkdown", "gfm", "testContext");

                var apiConnection = Substitute.For<IApiConnection>();
                apiConnection.Post<string>(Args.Uri, payload, "text/html", "text/plain")
                    .Returns(Task.FromResult(response));

                var client = new MiscellaneousClient(apiConnection);

                var html = await client.RenderArbitraryMarkdown(payload);
                Assert.Equal("<strong>Test</strong>", html);
                apiConnection.Received()
                    .Post<string>(Arg.Is<Uri>(u => u.ToString() == "markdown"),
                    payload,
                    "text/html",
                    "text/plain");
            }
        }
        public class TheGetEmojisMethod
        {
            [Fact]
            public async Task RequestsTheEmojiEndpoint()
            {
                IReadOnlyList<Emoji> response = new List<Emoji>
                {
                    { new Emoji("foo", "http://example.com/foo.gif") },
                    { new Emoji("bar", "http://example.com/bar.gif") }
                };

                var apiConnection = Substitute.For<IApiConnection>();
                apiConnection.GetAll<Emoji>(Args.Uri)
                          .Returns(Task.FromResult(response));

                var client = new MiscellaneousClient(apiConnection);

                var emojis = await client.GetAllEmojis();

                Assert.Equal(2, emojis.Count);
                Assert.Equal("foo", emojis[0].Name);
                apiConnection.Received()
                    .GetAll<Emoji>(Arg.Is<Uri>(u => u.ToString() == "emojis"));
            }
        }

        public class TheGetResourceRateLimitsMethod
        {
            [Fact]
            public async Task RequestsTheResourceRateLimitEndpoint()
            {
                var rateLimit = new MiscellaneousRateLimit(
                     new ResourceRateLimit(
                         new RateLimit(5000, 4999, 1372700873),
                         new RateLimit(30, 18, 1372700873)
                     ),
                     new RateLimit(100, 75, 1372700873)
                 );
                var apiConnection = Substitute.For<IApiConnection>();
                apiConnection.Get<MiscellaneousRateLimit>(Arg.Is<Uri>(u => u.ToString() == "rate_limit")).Returns(Task.FromResult(rateLimit));

                var client = new MiscellaneousClient(apiConnection);

                var result = await client.GetRateLimits();

                // Test the core limits
                Assert.Equal(5000, result.Resources.Core.Limit);
                Assert.Equal(4999, result.Resources.Core.Remaining);
                Assert.Equal(1372700873, result.Resources.Core.ResetAsUtcEpochSeconds);
                var expectedReset = DateTimeOffset.ParseExact(
                    "Mon 01 Jul 2013 5:47:53 PM -00:00",
                    "ddd dd MMM yyyy h:mm:ss tt zzz",
                    CultureInfo.InvariantCulture);
                Assert.Equal(expectedReset, result.Resources.Core.Reset);

                // Test the search limits
                Assert.Equal(30, result.Resources.Search.Limit);
                Assert.Equal(18, result.Resources.Search.Remaining);
                Assert.Equal(1372700873, result.Resources.Search.ResetAsUtcEpochSeconds);
                expectedReset = DateTimeOffset.ParseExact(
                    "Mon 01 Jul 2013 5:47:53 PM -00:00",
                    "ddd dd MMM yyyy h:mm:ss tt zzz",
                    CultureInfo.InvariantCulture);
                Assert.Equal(expectedReset, result.Resources.Search.Reset);

                // Test the depreciated rate limits
                Assert.Equal(100, result.Rate.Limit);
                Assert.Equal(75, result.Rate.Remaining);
                Assert.Equal(1372700873, result.Rate.ResetAsUtcEpochSeconds);
                expectedReset = DateTimeOffset.ParseExact(
                    "Mon 01 Jul 2013 5:47:53 PM -00:00",
                    "ddd dd MMM yyyy h:mm:ss tt zzz",
                    CultureInfo.InvariantCulture);
                Assert.Equal(expectedReset, result.Rate.Reset);

                apiConnection.Received()
                    .Get<MiscellaneousRateLimit>(Arg.Is<Uri>(u => u.ToString() == "rate_limit"));
            }
        }

        public class TheGetMetadataMethod
        {
            [Fact]
            public async Task RequestsTheMetadataEndpoint()
            {
                var meta = new Meta(
                     false,
                     "12345ABCDE",
                     new[] { "1.1.1.1/24", "1.1.1.2/24" },
                     new[] { "1.1.2.1/24", "1.1.2.2/24" },
                     new[] { "1.1.3.1/24", "1.1.3.2/24" },
                     new[] { "1.1.4.1", "1.1.4.2" }
                 );

                var apiConnection = Substitute.For<IApiConnection>();
                apiConnection.Get<Meta>(Arg.Is<Uri>(u => u.ToString() == "meta")).Returns(Task.FromResult(meta));
                var client = new MiscellaneousClient(apiConnection);

                var result = await client.GetMetadata();

                Assert.False(result.VerifiablePasswordAuthentication);
                Assert.Equal("12345ABCDE", result.GitHubServicesSha);
                Assert.Equal(result.Hooks, new[] { "1.1.1.1/24", "1.1.1.2/24" });
                Assert.Equal(result.Git, new[] { "1.1.2.1/24", "1.1.2.2/24" });
                Assert.Equal(result.Pages, new[] { "1.1.3.1/24", "1.1.3.2/24" });
                Assert.Equal(result.Importer, new[] { "1.1.4.1", "1.1.4.2" });

                apiConnection.Received()
                    .Get<Meta>(Arg.Is<Uri>(u => u.ToString() == "meta"));
            }
        }

        public class TheCtor
        {
            [Fact]
            public void EnsuresNonNullArguments()
            {
                Assert.Throws<ArgumentNullException>(() => new MiscellaneousClient(null));
            }
        }
    }
}
