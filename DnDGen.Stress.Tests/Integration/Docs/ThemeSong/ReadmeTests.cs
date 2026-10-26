using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DnDGen.Stress.Tests.Integration.Docs.ThemeSong
{
    internal partial class ReadmeTests
    {
        private const int SunoTitleLimit = 80;
        private const int SunoStyleLimit = 1000;
        private const int SunoLyricsLimit = 5000;

        private string[] _record;

        [OneTimeSetUp]
        public void Setup()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "docs", "theme-song", "README.md");
            _record = File.ReadAllLines(path);
        }

        [Test]
        public void HasSectionsInOrder()
        {
            var headings = GetLinesOutsideCodeBlocks(_record).Where(line => line.StartsWith("## "));
            Assert.That(headings, Is.EqualTo(
            [
                "## Updating",
                "## Title",
                "## Style",
                "## Lyrics",
                "## Sources",
                "## Suno Notes",
                "## Album Art",
            ]));
        }

        [TestCase("Title", SunoTitleLimit)]
        [TestCase("Style", SunoStyleLimit)]
        [TestCase("Lyrics", SunoLyricsLimit)]
        public void SunoField_FitsSunoLimit(string field, int limit)
        {
            var block = GetCodeBlock(field);
            Assert.That(block, Has.Length.LessThanOrEqualTo(limit).And.Not.Empty);
        }

        [Test]
        public void Sources_HasCorrectTable()
        {
            var sources = GetSection("Sources");
            var table = GetTable(sources);
            string[] columns = table.FirstOrDefault() ?? [];
            Assert.That(columns, Is.EqualTo(["Lyric", "From the README"]));

            string[] sourcedLyrics = [.. table.Skip(1).SelectMany(row => row[0].Split("<br>"))];
            Assert.That(sourcedLyrics, Is.EqualTo(GetLyricLines()), "Each lyric line needs one Sources row, in the order it is first sung");
        }

        private string[] GetLyricLines()
        {
            var lines = GetCodeBlock("Lyrics")
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Where(line => !line.StartsWith('['))
                .Where(line => !(line.StartsWith('(') && line.EndsWith(')')))
                .Distinct();

            return [.. lines];
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

        private List<string> GetSection(string heading)
        {
            var section = new List<string>();
            var inSection = false;
            var inCodeBlock = false;

            foreach (var line in _record)
            {
                if (line.StartsWith("```"))
                    inCodeBlock = !inCodeBlock;

                if (!inCodeBlock && line.StartsWith("## "))
                    inSection = line == $"## {heading}";
                else if (inSection)
                    section.Add(line);
            }

            return section;
        }

        private static List<string> GetCodeBlocks(List<string> section)
        {
            var blocks = new List<string>();
            List<string> block = null;

            foreach (var line in section)
            {
                if (!line.StartsWith("```"))
                {
                    block?.Add(line);
                }
                else if (block == null)
                {
                    block = [];
                }
                else
                {
                    blocks.Add(string.Join("\n", block));
                    block = null;
                }
            }

            return blocks;
        }

        private string GetCodeBlock(string field)
        {
            var section = GetSection(field);
            var blocks = GetCodeBlocks(section);
            Assert.That(blocks, Has.Exactly(1).Items, $"## {field} needs exactly one code block");

            return blocks[0];
        }

        private static List<string[]> GetTable(List<string> section)
        {
            var cells = section
                .Where(line => line.StartsWith('|'))
                .Select(GetCells)
                .Where(cells => !cells.All(cell => CellPartialRegex().IsMatch(cell)))
                .ToList();

            return [.. cells];
        }

        private static string[] GetCells(string row)
        {
            var cells = row.Trim().Trim('|').Split('|').Select(cell => cell.Trim());
            return [.. cells];
        }

        [GeneratedRegex("^:?-+:?$")]
        private static partial Regex CellPartialRegex();
    }
}
