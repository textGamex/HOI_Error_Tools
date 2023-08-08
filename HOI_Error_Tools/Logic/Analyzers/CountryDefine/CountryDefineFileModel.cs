﻿using System.Collections.Generic;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public partial class CountryDefineFileAnalyzer
{
    private sealed class CountryDefineFileModel
    {
        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<LeavesNode> SetPopularitiesList { get; }
        public IReadOnlyList<LeafValueNode> OwnIdeaNodes { get; }

        public IReadOnlyList<LeavesNode> SetPoliticsList { get; }

        public CountryDefineFileModel(Node rootNode)
        {
            SetPopularitiesList = ParseHelper.GetAllLeafKeyAndValueInAllNode(rootNode, "set_popularities").ToList();
            OwnIdeaNodes = ParseHelper.GetLeafValueNodesInAllNode(rootNode, "add_ideas").ToList();
            SetPoliticsList = ParseHelper.GetAllLeafKeyAndValueInAllNode(rootNode, "set_politics").ToList();
        }
    }
}