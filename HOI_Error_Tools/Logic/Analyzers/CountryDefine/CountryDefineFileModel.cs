using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public partial class CountryDefineFileAnalyzer
{
    public sealed class CountryDefineFileModel
    {
        public IReadOnlyList<LeavesNode> SetPopularitiesList { get; }
        public IReadOnlyList<LeafValueNode> OwnIdeaNodes { get; }
        public IReadOnlyList<LeafContent> OwnIdeaLeaves { get; }
        public IReadOnlyList<LeavesNode> SetPoliticsList { get; }
        public IReadOnlyList<LeafContent> Capitals { get; }
        public IReadOnlyList<LeafContent> Puppets { get; }
        public IReadOnlyList<LeafContent> CountriesTagOfAddToFaction { get; }
        public IReadOnlyList<LeavesNode> SetAutonomies { get; }
        public IReadOnlyList<LeavesNode> SetTechnologies { get; }
        public IReadOnlyList<LeafContent> GiveGuaranteeCountriesTag { get; }
        public IReadOnlyList<LeafContent> OwnCharacters { get; }
        public IReadOnlyList<LeafContent> OwnOobs { get; }
        
        private readonly Node _rootNode;

        private static readonly ImmutableHashSet<string> OwnOobsKeywords = 
            ImmutableHashSet.CreateRange(new []{"oob", "set_oob", "set_naval_oob", "set_air_oob"});
        private static readonly ImmutableHashSet<string> OwnCharactersKeywords = 
            ImmutableHashSet.CreateRange(new[] { "recruit_character", "promote_character", "retire_character" });
        private static readonly ImmutableHashSet<string> OwnIdeasKeywords = 
            ImmutableHashSet.CreateRange(new []{Keywords.AddIdeas, "remove_ideas"});
        private static readonly ImmutableHashSet<string> LeafContentsKeywords = ImmutableHashSet.CreateRange( 
            new []{Keywords.AddIdeas, Keywords.Capital, Keywords.Puppet, Keywords.GiveGuarantee, Keywords.AddToFaction});

        public CountryDefineFileModel(Node rootNode)
        {
            _rootNode = rootNode;

            SetPopularitiesList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_popularities").ToList();
            OwnIdeaNodes = GetOwnIdeas();
            SetPoliticsList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_politics").ToList();
            var leaves = ParseHelper.GetLeafContentsByKeywordsInChildren(rootNode, LeafContentsKeywords);
            OwnIdeaLeaves = leaves[Keywords.AddIdeas];
            Capitals = leaves[Keywords.Capital];
            Puppets = leaves[Keywords.Puppet];
            CountriesTagOfAddToFaction = leaves[Keywords.AddToFaction];
            GiveGuaranteeCountriesTag = leaves[Keywords.GiveGuarantee];
            SetAutonomies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_autonomy").ToList();
            SetTechnologies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_technology").ToList();
            OwnCharacters = GetOwnCharacters();
            OwnOobs = GetOwnOobs();
        }
        
        private IReadOnlyList<LeafContent> GetOwnOobs()
        {
            return ParseHelper.GetLeafContentsInChildren(_rootNode, OwnOobsKeywords).ToList();
        }

        private IReadOnlyList<LeafValueNode> GetOwnIdeas()
        {
            return ParseHelper.GetLeafValueNodesInChildren(_rootNode, OwnIdeasKeywords).ToList();
        }
        
        private IReadOnlyList<LeafContent> GetOwnCharacters()
        {
            return ParseHelper.GetLeafContentsInChildren(_rootNode, OwnCharactersKeywords).ToList();
        }
        
        private static class Keywords
        {
            public const string AddIdeas = "add_ideas";
            public const string Capital = "capital";
            public const string Puppet = "puppet";
            public const string GiveGuarantee = "give_guarantee";
            public const string AddToFaction = "add_to_faction";
        }
    }
}