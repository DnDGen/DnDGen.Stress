using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DnDGen.Stress.Tests.Integration.Docs.ThemeSong
{
    internal class ReadmeTests
    {
        private const int SunoTitleLimit = 80;
        private const int SunoStyleLimit = 1000;

        private string[] _record;

        [OneTimeSetUp]
        public void Setup()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "docs", "theme-song", "README.md");
            _record = File.ReadAllLines(path);
        }

        [Test]
        public void Record_HasSectionsInOrder()
        {
            var headings = GetLinesOutsideCodeBlocks(_record).Where(line => line.StartsWith("## "));
            Assert.That(headings, Is.EqualTo(new[]
            {
                "## Updating",
                "## Title",
                "## Style",
                "## Lyrics",
                "## Sources",
                "## Suno Notes",
                "## Album Art",
            }));
        }

        [TestCase("Title")]
        [TestCase("Style")]
        [TestCase("Lyrics")]
        public void SunoField_HasOneCodeBlock(string field)
        {
            var blocks = GetCodeBlocks(GetSection(field));
            Assert.That(blocks, Has.Exactly(1).Items);
        }

        [Test]
        public void Sources_HasLyricAndFromTheReadmeColumns()
        {
            string[] columns = GetTable(GetSection("Sources")).FirstOrDefault() ?? [];
            Assert.That(columns, Is.EqualTo(new[] { "Lyric", "From the README" }));
        }

        [Test]
        public void Title_FitsSunoLimit()
        {
            var title = GetCodeBlock("Title");
            Assert.That(title.Length, Is.LessThanOrEqualTo(SunoTitleLimit), title);
        }

        [Test]
        public void Style_FitsSunoLimit()
        {
            var style = GetCodeBlock("Style");
            Assert.That(style.Length, Is.LessThanOrEqualTo(SunoStyleLimit), style);
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
            var blocks = GetCodeBlocks(GetSection(field));
            Assert.That(blocks, Has.Exactly(1).Items, $"## {field} needs exactly one code block");

            return blocks[0];
        }

        private static List<string[]> GetTable(List<string> section)
        {
            return section
                .Where(line => line.StartsWith('|'))
                .Select(GetCells)
                .Where(cells => !cells.All(cell => Regex.IsMatch(cell, "^:?-+:?$")))
                .ToList();
        }

        private static string[] GetCells(string row) => row.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();
    }
}
