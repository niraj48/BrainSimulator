﻿using GoodAI.Core.Utils;
using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.Drawing;

namespace GoodAI.Modules.School.LearningTasks
{
    /// <author>GoodAI</author>
    /// <meta>Os</meta>
    /// <status>WIP</status>
    /// <summary>"LTObstacles with POW visible target" learning task</summary>
    /// <description>
    /// The class is derived from LTObstacles, and implements the level where the target is always visible from POW and randomness Check LTObstacles for further details
    /// </description>
    public class LTObstaclesTargetOnSight : LTObstacles                           // Deriving from LTObstacles
    {
        public LTObstaclesTargetOnSight() { }

        public LTObstaclesTargetOnSight(SchoolWorld w)
            : base(w)
        {
            TSProgression.Clear();
            //TSProgression.Add(TSHints.Clone());

            TSHints[OBSTACLES_LEVEL] = 7;
            TSProgression.Add(OBSTACLES_LEVEL, 7);

            SetHints(TSHints);
        }


    }
}