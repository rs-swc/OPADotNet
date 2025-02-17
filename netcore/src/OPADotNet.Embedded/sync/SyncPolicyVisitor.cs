﻿/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using OPADotNet.Ast;
using OPADotNet.Ast.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OPADotNet.Embedded.sync
{
    /// <summary>
    /// This visitor reads a policy and figures out its name and the data it references.
    /// </summary>
    internal class SyncPolicyVisitor : AstVisitor<SyncPolicy>
    {
        private static SyncPathVisitor pathVisitor = new SyncPathVisitor();
        public override SyncPolicy VisitPolicy(AstPolicy astPolicy)
        {
            string path = pathVisitor.Visit(astPolicy.Package);

            SyncDataVisitor syncDataVisitor = new SyncDataVisitor();
            syncDataVisitor.Visit(astPolicy.Rules);

            return new SyncPolicy()
            {
                DataSets = syncDataVisitor.DataSets,
                PolicyName = path
            };
        }
    }
}
