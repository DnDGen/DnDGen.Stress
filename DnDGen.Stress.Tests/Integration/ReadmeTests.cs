using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DnDGen.Stress.Tests.Integration
{
    internal partial class ReadmeTests
    {
        private const string ThemeSongBadge = "[![Theme Song](https://img.shields.io/badge/theme_song-Suno-ff69b4)]";
        private const string RecordUrl = "https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md";
        private const BindingFlags DeclaredPublicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static readonly Type[] PublicTypes = [typeof(Stressor), typeof(StressorOptions)];

        private string[] _readme;

        [OneTimeSetUp]
        public void Setup()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "README.md");
            _readme = File.ReadAllLines(path);
        }

        [Test]
        public void ThemeSong_HasBadgeBesideTheBuildBadge()
        {
            var buildBadgeLine = _readme.FirstOrDefault(line => line.StartsWith("[![Build Status]")) ?? string.Empty;
            Assert.That(buildBadgeLine, Does.Contain($" {ThemeSongBadge}(https://suno.com/s/"));
        }

        [Test]
        public void ThemeSong_HasSectionAtTheEnd()
        {
            var headings = GetLinesOutsideCodeBlocks(_readme).Where(line => line.StartsWith("## "));
            Assert.That(headings.LastOrDefault(), Is.EqualTo("## Theme Song"));
        }

        [Test]
        public void ThemeSong_LinksToTheRecord()
        {
            var links = GetLinks(_readme);
            Assert.That(links, Does.Contain(RecordUrl));

            var recordPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "docs", "theme-song", "README.md");
            Assert.That(recordPath, Does.Exist);
        }

        [Test]
        public void ThemeSong_LinksToTheSameSongAsTheRecord()
        {
            var recordPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "docs", "theme-song", "README.md");
            var record = File.ReadAllLines(recordPath);
            string[] recordSongs = [.. GetLinks(record).Where(IsSunoLink).Distinct()];
            string[] readmeSongs = [.. GetLinks(_readme).Where(IsSunoLink).Distinct()];

            Assert.That(recordSongs, Has.Exactly(1).Items, "The record needs to link to one song");
            Assert.That(readmeSongs, Is.EqualTo(recordSongs));
        }

        [Test]
        public void DocumentLinks_AreAbsolute()
        {
            var relativeLinks = GetLinks(_readme).Where(link => !link.StartsWith("http://") && !link.StartsWith("https://"));
            Assert.That(relativeLinks, Is.Empty, "Relative links don't work on NuGet");
        }

        [Test]
        public void Constructors_AreAllDocumented()
        {
            var constructors = PublicTypes.SelectMany(type => type.GetConstructors()).Select(WriteMember);
            Assert.That(GetDocumented("Constructor"), Is.EquivalentTo(constructors));
        }

        [Test]
        public void Methods_AreAllDocumented()
        {
            var methods = PublicTypes
                .SelectMany(type => type.GetMethods(DeclaredPublicMembers))
                .Where(method => !method.IsSpecialName)
                .Select(WriteMember);

            Assert.That(GetDocumented("Method"), Is.EquivalentTo(methods));
        }

        [Test]
        public void Properties_AreAllDocumented()
        {
            var fields = PublicTypes.SelectMany(type => type.GetFields(DeclaredPublicMembers)).Select(field => field.Name);
            var readOnlyProperties = PublicTypes
                .SelectMany(type => type.GetProperties(DeclaredPublicMembers))
                .Where(property => property.SetMethod?.IsPublic != true)
                .Select(property => property.Name);

            Assert.That(GetDocumented("Property"), Is.EquivalentTo(fields.Concat(readOnlyProperties)));
        }

        private string[] GetDocumented(string kind) => [.. GetTableRows(kind).Select(row => row[0].Trim('`'))];

        private IEnumerable<string[]> GetTableRows(string kind)
        {
            string[] header = [];

            foreach (var line in GetLinesOutsideCodeBlocks(_readme))
            {
                if (!line.StartsWith('|'))
                {
                    header = [];
                    continue;
                }

                string[] cells = [.. line.Trim().Trim('|').Split('|').Select(cell => cell.Trim())];

                if (header.Length == 0)
                    header = cells;
                else if (header[0] == kind && !cells[0].StartsWith("---"))
                    yield return cells;
            }
        }

        private static string WriteMember(MethodBase member)
        {
            var name = member is ConstructorInfo ? member.DeclaringType.Name : member.Name;
            var parameters = member.GetParameters().Select(parameter => parameter.Name);

            return $"{name}({string.Join(", ", parameters)})";
        }

        private static bool IsSunoLink(string link) => link.StartsWith("https://suno.com/");

        private static string[] GetLinks(IEnumerable<string> lines)
        {
            var links = GetLinesOutsideCodeBlocks(lines)
                .SelectMany(line => MarkdownLinkPartialRegex().Matches(line).Concat(HtmlLinkPartialRegex().Matches(line)))
                .Select(match => match.Groups[1].Value);

            return [.. links];
        }

        private static IEnumerable<string> GetLinesOutsideCodeBlocks(IEnumerable<string> lines)
        {
            var inCodeBlock = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("```"))
                    inCodeBlock = !inCodeBlock;
                else if (!inCodeBlock)
                    yield return line;
            }
        }

        [GeneratedRegex(@"\]\(([^)\s]+)\)")]
        private static partial Regex MarkdownLinkPartialRegex();

        [GeneratedRegex(@"(?:href|src)=""([^""]+)""")]
        private static partial Regex HtmlLinkPartialRegex();
    }
}
